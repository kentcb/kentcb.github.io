---
title: Disconnected Layers
assets: /assets/2016-06-07-disconnected-layers/
tags: [ "Design", "C#", ".NET", "Xamarin" ]
---

This post describes a technique I use in pretty much every non-trivial application I write these days. I call it a "disconnected layer". Essentially, it's an abstraction that allows you to quickly swap out your application's service layer. By flicking a switch, you can run your application against a simulated back-end.

I pioneered this technique when I was working for large investment banks (is there any other kind?). I was writing a WPF front-end for their FX traders. It had to connect to a large variety of back-end infrastructure. I realised that relying on all those back-end systems was going to be a pain point since they were in a constant state of flux.

These days I write mostly mobile applications and my reasons for implementing disconnected layers has shifted somewhat.

## Motivation

### Reliable, repeatable data

Your disconnected layer need not save data permanently. It can simply store it in memory. Thus, upon restarting the application your state will be back where it was. This is handy when you're working on a bug that is triggered by users entering certain data because you can enter the same thing each time rather than trying to avoid clashes and such with old data.

### Simulate problemmatic data sets

When you have a problem that is known to be triggered by a certain data set, you can literally copy that data set into your disconnected layer. You then run up the app and prove the repro. From there you can more confidently identify the bug. Having identified the bug, you can set about writing a unit test that also fails, before you then fix the issue.

I have also used this technique when reproducing performance problems. I simply took the production data set and had my disconnected layer return it. I therefore had an instant, repeatable repro for the problem and could move forward with a fix.

### Reduce development cycle times

As you'll see below, your disconnected layer can go beyond returning pre-canned data. It can also simulate delays. My disconnected layers always take a Boolean flag indicating whether to include fake, random delays or not.

Whilst developing a feature for an application, I will generally have delays turned off. This gets me a shorter edit/debug cycle because the UI responds as quickly as possible. Once I near completion of the feature, I'll turn simulated delays on to ensure that activity indicators and such in the UI work as intended. Finally, I'll switch to connected mode to verify it works against the real back-end (assuming a real back-end exists).

### Forge ahead of the back-end team

Several projects I've worked on have been done in parallel, or ahead of, the back-end implementation. Using a disconnected layer can allow you to circumvent the inevitable roadblocks that you would otherwise experience in these situations.

Of course, it's important that you don't stray far from the eventual back-end design. In some cases you may even be responsible for designing the back-end APIs. Implementing a disconnected layer can help you flesh out those designs before committing publicly. 

### Work offline

I'm on a plane right now, and as a remote worker this is quite often the case. Being able to pick up where I left off in the office is super handy. On top of that, I live in Australia where our Internet infrastructure is, frankly, a joke. Whenever Telstra inexplicably decide to remove the stability profile from my line (because why would I want my Internet to be *stable*?) I can continue working whilst their support goes through the rudiments of having it turned back on.

### Avoid unreliable back-ends

I touched on this above with respect to the banking systems I had to integrate with. In my case there was too much yoyo-ing of dev/integration environments by other teams, so having to rely on them meant I could hit a brick wall half way through working on a feature or problem.

Of course, there are a range of reasons _why_ your back-end might be unreliable: in development, flakey network, frequent restarts *etcetera*. Whatever the reason, this technique will unblock you.

### Simulate unreliable back-ends

This seems like a contradiction when contrasted with the above point, but it isn't. Your disconnected layer can simulate errors to improve the robustness of consuming code. I always include a Boolean flag in my disconnected layer that dictates whether APIs should randomly fail. I tend to do the bulk of the development work with the flag off (because I'm concerned with just getting the implementation done and don't want random errors in the mix yet), then I'll turn it on towards the end of development when I'm double-checking edge cases and error handling.

## Implementation

Hopefully by now I've convinced you that a disconnected layer is A Good Thing. So now let me explain how I implement mine. It's important to realise that this is more a pattern than a prescription. Your implementation may differ quite greatly from mine, or from application to application. Indeed, the disconnected layer I put together for the WPF application was quite different to those I put together for mobile applications.

These are the basic ingredients of a disconnected layer:

* an abstraction, or set of abstractions, for working with back-end systems
* a "connected" implementation of the abstractions
* a "disconnected" implementation of the abstractions
* a means of switching between implementations

### The abstraction

For mobile applications, my abstraction is typically a single interface. It's the same interface I pass into [Refit](https://github.com/paulcbetts/refit) to generate my web API interaction layer.

Here's a simple example:

```csharp
public interface IRawWebApi
{
    [Post("/Auth")]
    IObservable<AuthenticateResponse> Authenticate(
        [Body] AuthenticateRequest request);

    [Get("/Users/{id}")]
    IObservable<User> GetUser(
        [Header("Authorization")]
        string token,
        int id);
}
```

I stick all types comprising my abstraction inside a **Services** assembly.

### The "connected" implementation

As I mentioned above, I typically don't need to do anything to get my connected layer. I just pass the interface into Refit and it does the rest. If you're not using Refit, I'll assume it's because you're not doing JSON-based web APIs with .NET (if so, my advice would be to just use Refit). For example, if my mobile application includes location tracking, I'd define a separate interface for that:

```csharp
public interface ILocationService
{
    IObservable<Geolocation> GetLocation(TimeSpan timeout = null);
}
```

Then, in my "connected" services project (which I normally call **Services.Connected**) I include the "real" implementation of this interface.

### The "disconnected" implementation

Alongside your **Services** and **Services.Connected** projects, you can create a **Services.Disconnected** project. Naturally, this is where the magic happens. Whilst the implementation is inevitably project-specific, I'll show an example of what mine typically looks like:

```csharp
public sealed class RawWebApi : IRawWebApi
{
    private readonly includeRandomDelays;
    private readonly includeRandomErrors;
    private readonly IImmutableList<User> users;

    public RawWebApi(bool includeRandomDelays, bool includeRandomErrors)
    {
        this.includeRandomDelays = includeRandomDelays;
        this.includeRandomErrors = includeRandomErrors;
        this.users = this.LoadUsers();
    }

    public IObservable<AuthenticateResponse> Authenticate(AuthenticateRequest request)
    {
        var response = new AuthenticateResponse
        {
            Token = Guid.NewGuid().ToString(),
            Expires = DateTime.UtcNow.AddHours(2)
        };

        return this
            .ErrorWithProbability(5)
            .SelectMany(
                _ =>
                    Observable
                        .Return(response)
                        .DelayIf(this.includeRandomDelays, 500, 1000));
    }

    public IObservable<User> GetUser(string token, int id)
    {
        var response = this
            .users
            .FirstOrDefault(user => user.Id == id);


        return this
            .ErrorWithProbability(5)
            .SelectMany(
                _ =>
                    Observable
                        .Return(response)
                        .DelayIf(this.includeRandomDelays, 100, 250));
    }

    ...
}
```

If you've not done much Reactive Extensions, this code may feel a bit strange. But it's about the pattern, not the implementation. You can also do this with the TPL or whatever you like (but I'd really recommend looking deeper into Rx).

As you can see, I have several helper methods that I've elided for clarity (and again, because this is about the pattern, not my specific implementation). The `ErrorWithProbability` method just fails randomly according to some specified probability (expressed here as a percentage). The `DelayIf` helper method delays the pipeline if the first parameter is `true`. The delay is a random number of milliseconds between the range dictated by the second and third parameters.

I've also left out my `LoadUsers` implementation. Normally I just stick JSON data into a resource file and deserialize it. By using the exact same format as what comes out of your production APIs, it makes it copy+paste-easy to use production data in your disconnected layer.

### The switch

Now that we have our abstraction and two implementations thereof, we need a means of switching between them. For my WPF application, I had a `serviceMode` configuration parameter in my *app.settings*. This parameter was used by my MEF composition logic to filter out parts. That is, setting `serviceMode` to `"disconnected"` would ensure the part catalog included disconnected services and not their connected counterparts. Thus any dependent parts would have the disconnected variant of the service injected.

For mobile apps I instead use a compiler symbol. For example, I might have `DISCONNECTED`, `DISCONNECTED_FAST`, and `DISCONNECTED_ERRORS` symbols. The composition logic then uses these symbols to `ifdef` in (or out) the appropriate components:

```csharp
private IRawWebApi CreateRawWebApi()
{
#if DISCONNECTED
    return new Services.Disconnected.RawWebApi(
#if DISCONNECTED_FAST
        true,
#else
        false,
#endif
#if DISCONNECTED_ERRORS
        true
#else
        false
#endif
    );
#else

    var httpClient = new HttpClient(new NativeMessageHandler())
    {
        BaseAddress = new Uri(baseUri)
    };

    return RestService.For<WebApi.IRawWebApi>(httpClient);
#endif
}
```

Certainly, the nested `ifdef`s make this harder to read than I'd like, but there's precisely one point in the code where this is necessary (in your composition logic), and you could always re-structure the code if you find it a burden.

## Conclusion

The technique of defining a disconnected layer has a slew of advantages. I didn't even discuss disadvantages in this post because I can only really think of the one - having to provide an implementation of the disconnected layer - and it's only minor when contrasted with the benefits discussed. I've used disconnected layers for a long time and would not want to write any non-trivial application without one.