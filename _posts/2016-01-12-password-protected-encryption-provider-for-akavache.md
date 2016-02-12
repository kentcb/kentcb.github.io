---
title: Password-protected Encryption Provider for Akavache
assets: /assets/2016-01-12-password-protected-encryption-provider-for-akavache/
tags: [ "xamarin", "akavache", "mobile" ]
---
If you've not heard of [Akavache](https://github.com/akavache/Akavache), it's a fantastic library from [Paul Betts](https://github.com/paulcbetts). In short, it's a persistent cache with a powerful, asynchronous API. It works pretty much everywhere, including on Xamarin platforms.

Akavache stores data in so-called blob caches. By default, data stored in these blob caches is not encrypted. However, by providing an implementation of [`IEncryptionProvider`](https://github.com/akavache/Akavache/blob/master/Akavache/Portable/IEncryptionProvider.cs), one can encrypt data before it is persisted, and decrypt it on the way back out:

![IEncryptionProvider]({{ page.assets }}encryption-provider.png "IEncryptionProvider")

Akavache itself comes with only one implementation of this interface, and it simply uses the [`ProtectedData`](https://msdn.microsoft.com/en-us/library/system.security.cryptography.protecteddata.aspx) class to encrypt and decrypt data. However, `ProtectedData` is not available on Xamarin. For this reason, Akavache defines its own `ProtectedData` implementation for such platforms, and the implementation simply passes through data. Thus, Akavache's "secure" blob cache is not at all secure on platforms that do not include a `ProtectedData` implementation.

Because my client was concerned about sensitive data being acquired from lost or stolen devices, I needed a solution to this problem.

## The Solution

You'll notice from the above class diagram that `IEncryptionProvider` includes no notion of a key or password. For that reason, I first needed my own interface via which a password could be provided:

```csharp
public interface IPasswordProtectedEncryptionProvider : IEncryptionProvider
{
    void SetPassword(string password);
}
```

This gives my consuming code a means of supplying the password entered by users. Here is the implementation of this interface:

```csharp
public sealed class PasswordProtectedEncryptionProvider : IPasswordProtectedEncryptionProvider
{
    private static readonly byte[] salt = Encoding.ASCII.GetBytes(# add a random, 16 character string here #);
    private readonly IScheduler scheduler;
    private readonly SymmetricAlgorithm symmetricAlgorithm;
    private ICryptoTransform decryptTransform;
    private ICryptoTransform encryptTransform;

    public PasswordProtectedEncryptionProvider(IScheduler scheduler)
    {
        scheduler.AssertNotNull(nameof(scheduler));

        this.scheduler = scheduler;
        this.symmetricAlgorithm = new RijndaelManaged();
    }

    public void SetPassword(string password)
    {
        password.AssertNotNull(nameof(password));

        var derived = new Rfc2898DeriveBytes(password, salt);
        var bytesForKey = this.symmetricAlgorithm.KeySize / 8;
        var bytesForIV = this.symmetricAlgorithm.BlockSize / 8;
        this.symmetricAlgorithm.Key = derived.GetBytes(bytesForKey);
        this.symmetricAlgorithm.IV = derived.GetBytes(bytesForIV);
        this.decryptTransform = this.symmetricAlgorithm.CreateDecryptor(this.symmetricAlgorithm.Key, this.symmetricAlgorithm.IV);
        this.encryptTransform = this.symmetricAlgorithm.CreateEncryptor(this.symmetricAlgorithm.Key, this.symmetricAlgorithm.IV);
    }

    public IObservable<byte[]> DecryptBlock(byte[] block)
    {
        block.AssertNotNull(nameof(block));

        if (this.decryptTransform == null)
        {
            return Observable.Throw<byte[]>(new InvalidOperationException("You must call SetPassword first."));
        }

        return Observable
            .Start(
                () => InMemoryTransform(block, this.decryptTransform),
                this.scheduler);
    }

    public IObservable<byte[]> EncryptBlock(byte[] block)
    {
        block.AssertNotNull(nameof(block));

        if (this.encryptTransform == null)
        {
            return Observable.Throw<byte[]>(new InvalidOperationException("You must call SetPassword first."));
        }

        return Observable
            .Start(
                () => InMemoryTransform(block, this.encryptTransform),
                this.scheduler);
    }

    private static byte[] InMemoryTransform(byte[] bytesToTransform, ICryptoTransform transform)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(bytesToTransform, 0, bytesToTransform.Length);
            }

            return memoryStream.ToArray();
        }
    }
}
```

In short, constructing requires only a scheduler. The `SetPassword` implementation uses `Rfc2898DeriveBytes` to generate the key and IV for a symmetric encryption algorithm. It also caches the crypto transforms that will be used during encryption and decryption, so as to avoid recreating them every call. Whenever a request for encryption or decryption is received, the scheduler provided during construction is used to queue the work.

The upshot is that I can provide this implementation to Akavache. When a user logs on, I call `SetPassword` using a combination of their user name and password. If the provided password is a match for any existing data in the blob cache, decryption succeeds and all is well. If the password is a mismatch, decryption fails with an exception and my application responds accordingly (by failing to log in, in my case). 

Note that whilst this code works on both iOS and Android, it is not portable (due to the use of security types like `Rfc2898DeriveBytes`). However, the code itself can be built for both iOS and Android, so it is portable source but not portable binary.