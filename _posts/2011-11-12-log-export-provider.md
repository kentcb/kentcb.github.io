---
title: LogExportProvider
assets: /assets/2011-11-12-log-export-provider/
tags: [ ".NET", "MEF", "Prism" ]
---
We all know how important logging is to any non-trivial application, so it stands to reason that we should make it as pain free as possible to add logging to our application components. But at the same time, we don't want to sacrifice too much, such as having to deal with a sub-standard logging API, or with a logging infrastructure that doesn't log the obvious stuff on our behalf.

Wouldn't it be nice if logging were as simple as this:

```csharp
namespace SomeNamespace
{
    public class SomeClass
    {
        [ImportingConstructor]
        public SomeClass(ILoggerService loggerService)
        {
            loggerService.Debug("Created an instance of {0}.", GetType().Name);
        }
    }
}
```

And, importantly, the resultant log entry looked something like this:

```xml
[2011-11-12 12:09:04,749] [1] [DEBUG] [SomeNamespace.SomeClass] Created an instance of SomeClass.
```

Prism ships with an `ILoggerFacade` interface that looks like this:

```csharp
public interface ILoggerFacade 
{ 
    void Log(string message, Category category, Priority priority); 
}
```

This is obviously a very bare-bones interface. Every time you write a log statement you'll be forced to:

* format any parameters yourself
* specify the category as a parameter (instead of having separate methods for each category)
* choose and specify a priority, even if it makes no sense for your code
* incur the cost of any preparation for the log statement even if the pertinent log level is disabled because you have no way to check whether it is or not

This is not very consumer-friendly at all, and you'll more than likely begin to find logging a more onerous task than it should be.

Another problem with using `ILoggerFacade` in Prism is the lack of any originating source in the output. There is nothing intrinsic to differentiate log statements from different components. If two components log the same message ("Initialized", for example), you will have no way to tell which component was initialized!

These problems forced me to come up with a custom solution. I wanted my logging to be based on log4net, and I wanted MEF to provide my components with a logging service instance. Moreover, that service must produce log entries that are specific to my component.

The first problem (poor API) was easiest to solve. I defined my own interface as follows:

```csharp
public interface ILoggerService 
{ 
    bool IsVerboseEnabled 
    { 
        get; 
    }
 
    bool IsDebugEnabled 
    { 
        get; 
    }
 
    bool IsInfoEnabled 
    { 
        get; 
    }
 
    bool IsWarnEnabled 
    { 
        get; 
    }
 
    bool IsErrorEnabled 
    { 
        get; 
    }
 
    bool IsPerfEnabled 
    { 
        get; 
    }
 
    void Verbose(string message);
 
    void Verbose(string message, Exception exception);
 
    void Verbose(string message, params object[] args);
 
    void Debug(string message);
 
    void Debug(string message, Exception exception);
 
    void Debug(string message, params object[] args);
 
    void Info(string message);
 
    void Info(string message, Exception exception);
 
    void Info(string message, params object[] args);
 
    void Warn(string message);
 
    void Warn(string message, Exception exception);
 
    void Warn(string message, params object[] args);
 
    void Error(string message);
 
    void Error(string message, Exception exception);
 
    void Error(string message, params object[] args);
 
    IDisposable Perf(string message);
 
    IDisposable Perf(string message, params object[] args); 
}
```

As you can see, this interface provides many overloads for all the relevant combinations of parameters you might need. This saves you, the caller, from having to deal with the annoyance of formatting messages or exceptions. There are also properties that can be used to check whether a given log level is enabled, which can be crucial in performance-critical paths. Finally, notice the handy `Perf` overloads which can be used to measure the performance of a block of code like this:

```csharp
using (loggerService.Perf("Authenticating the user")) 
{ 
    // do authentication here
}
```

The only thing I haven't included (because I haven't needed it) are generic `Write` methods that take the log level as a parameter instead of inferring the log level from the method name. Such methods can be useful in dynamic logging scenarios, so you may want to add your own.

With the API defined, it was time to write an implementation:

```csharp
public sealed class Log4NetLoggerService : ILoggerService 
{ 
    private static readonly Level perfLevel = new Level(35000, "PERF"); 
    private readonly ILog log;
 
    public Log4NetLoggerService(ILog log) 
    { 
        log.AssertNotNull("log"); 
        this.log = log; 
    }
 
    public bool IsVerboseEnabled 
    { 
        get { return this.log.Logger.IsEnabledFor(Level.Verbose); } 
    }
 
    public bool IsDebugEnabled 
    { 
        get { return this.log.IsDebugEnabled; } 
    }
 
    public bool IsInfoEnabled 
    { 
        get { return this.log.IsInfoEnabled; } 
    }
 
    public bool IsWarnEnabled 
    { 
        get { return this.log.IsWarnEnabled; } 
    }
 
    public bool IsErrorEnabled 
    { 
        get { return this.log.IsErrorEnabled; } 
    }
 
    public bool IsPerfEnabled 
    { 
        get { return this.log.Logger.IsEnabledFor(perfLevel); } 
    }
 
    public void Verbose(string message) 
    { 
        this.log.Logger.Log(typeof(Log4NetLoggerService), Level.Verbose, message, null); 
    }
 
    public void Verbose(string message, Exception exception) 
    { 
        this.log.Logger.Log(typeof(Log4NetLoggerService), Level.Verbose, message, exception); 
    }
 
    public void Verbose(string message, params object[] args) 
    { 
        this.log.Logger.Log(typeof(Log4NetLoggerService), Level.Verbose, new SystemStringFormat(CultureInfo.InvariantCulture, message, args), null); 
    }
 
    public void Debug(string message) 
    { 
        this.log.Debug(message); 
    }
 
    public void Debug(string message, Exception exception) 
    { 
        this.log.Debug(message, exception); 
    }
 
    public void Debug(string message, params object[] args) 
    { 
        this.log.DebugFormat(CultureInfo.InvariantCulture, message, args); 
    }
 
    public void Info(string message) 
    { 
        this.log.Info(message); 
    }
 
    public void Info(string message, Exception exception) 
    { 
        this.log.Info(message, exception); 
    }
 
    public void Info(string message, params object[] args) 
    { 
        this.log.InfoFormat(CultureInfo.InvariantCulture, message, args); 
    }
 
    public void Warn(string message) 
    { 
        this.log.Warn(message); 
    }
 
    public void Warn(string message, Exception exception) 
    { 
        this.log.Warn(message, exception); 
    }
 
    public void Warn(string message, params object[] args) 
    { 
        this.log.WarnFormat(CultureInfo.InvariantCulture, message, args); 
    }
 
    public void Error(string message) 
    { 
        this.log.Error(message); 
    }
 
    public void Error(string message, Exception exception) 
    { 
        this.log.Error(message, exception); 
    }
 
    public void Error(string message, params object[] args) 
    { 
        this.log.ErrorFormat(CultureInfo.InvariantCulture, message, args); 
    }
 
    public IDisposable Perf(string message) 
    { 
        message.AssertNotNull("message"); 
        return new PerfBlock(this, message); 
    }
 
    public IDisposable Perf(string message, params object[] args) 
    { 
        message.AssertNotNull("message"); 
        args.AssertNotNull("args"); 
        return new PerfBlock(this, string.Format(CultureInfo.InvariantCulture, message, args)); 
    }
 
    private sealed class PerfBlock : IDisposable 
    { 
        private readonly Log4NetLoggerService owner; 
        private readonly string message; 
        private readonly Stopwatch stopwatch; 
        private bool disposed;
 
        public PerfBlock(Log4NetLoggerService owner, string message) 
        { 
            this.owner = owner; 
            this.message = message; 
            this.stopwatch = Stopwatch.StartNew(); 
        }
 
        public void Dispose() 
        { 
            if (!this.disposed) 
            { 
                this.disposed = true; 
                this.stopwatch.Stop(); 
                var messageWithTimingInfo = string.Format(CultureInfo.InvariantCulture, "{0} [{1}, {2}ms]", this.message, this.stopwatch.Elapsed, this.stopwatch.ElapsedMilliseconds); 
                this.owner.log.Logger.Log(typeof(Log4NetLoggerService), perfLevel, messageWithTimingInfo, null); 
            } 
        } 
    } 
}
```

It's all pretty straightforward - most of the code just delegates to log4net.

But notice how the constructor requires a `log4net.ILog`? Log4net provides various ways by which an `ILog` can be obtained, but we would like to use `LogManager.GetLogger(Type ownerType)`, where `ownerType` is the type importing our service. How, then, can we expect MEF to provide instances of `ILoggerService` when our constructor has a dependency that it cannot satisfy?

MEF supports an abstraction called `ExportProvider`, which is an object that can dynamically provide exports to satisfy matching imports. The trick to making this all work seamlessly is a custom `ExportProvider` that creates instances of `Log4NetLoggerService` on the fly to satisfy imports of type `ILoggerService`. In order to create `Log4NetLoggerService` instances, the export provider must know the type of the object that is importing the service. Thankfully, MEF supports obtaining this information through its reflection services.

Here is the code for our custom `ExportProvider`:

```csharp
public sealed class LoggerServiceExportProvider : ExportProvider 
{ 
    private static readonly ILoggerService log = new Log4NetLoggerService(LogManager.GetLogger(typeof(LoggerServiceExportProvider))); 
    private readonly IDictionary<Type, ILoggerService> loggerServiceCache;
 
    public LoggerServiceExportProvider() 
    { 
        this.loggerServiceCache = new Dictionary<Type, ILoggerService>(); 
    }
 
    protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition) 
    { 
        var contractName = definition.ContractName;
 
        log.Verbose("Attempting to resolve contract name '{0}' to a log instance.", contractName);
 
        if (string.IsNullOrEmpty(contractName)) 
        { 
            log.Verbose("Contract name is null or empty - cannot resolve."); 
            yield break; 
        }
 
        if (!string.Equals(typeof(ILoggerService).FullName, contractName, StringComparison.Ordinal)) 
        { 
            log.Verbose("Incorrect contract - cannot resolve."); 
            yield break; 
        }
 
        if (definition.Cardinality != ImportCardinality.ExactlyOne) 
        { 
            log.Verbose("Cardinality is {0} - cannot resolve.", definition.Cardinality); 
            yield break; 
        }
 
        // in order to get a log4net logger, we need the type importing the logger facade 
        Type ownerType = null;
 
        if (ReflectionModelServices.IsImportingParameter(definition)) 
        { 
            log.Verbose("Parameter import detected.");
 
            var importingParameter = ReflectionModelServices.GetImportingParameter(definition); 
            ownerType = importingParameter.Value.Member.DeclaringType; 
        } 
        else
        { 
            log.Verbose("Property import detected.");
 
            var setAccessor = ReflectionModelServices 
                .GetImportingMember(definition) 
                .GetAccessors() 
                .Where(x => x is MethodInfo) 
                .Select(x => x as MethodInfo) 
                .FirstOrDefault(x => (x.Attributes & MethodAttributes.SpecialName) == MethodAttributes.SpecialName && x.Name.StartsWith("set_", StringComparison.Ordinal));
 
            if (setAccessor == null) 
            { 
                log.Verbose("Set accessor for property not found - cannot resolve."); 
                yield break; 
            }
 
            ownerType = setAccessor.DeclaringType; 
        }
 
        if (ownerType == null) 
        { 
            log.Verbose("Owner type could not be determined - cannot resolve."); 
            yield break; 
        }
 
        log.Verbose("Owner type is '{0}'.", ownerType.FullName);
 
        ILoggerService loggerService;
 
        if (!this.loggerServiceCache.TryGetValue(ownerType, out loggerService)) 
        { 
            log.Verbose("Logger facade for owner type '{0}' is not yet cached - creating it.", ownerType.FullName);
 
            var logInstance = LogManager.GetLogger(ownerType); 
            loggerService = new Log4NetLoggerService(logInstance); 
            this.loggerServiceCache[ownerType] = loggerService; 
        }
 
        var export = new Export(contractName, () => loggerService); 
        yield return export; 
    } 
}
```

There are several things to note about this implementation:

* log service instances are cached. This means that if different instances of the same type import a logger service, they will (quickly) get the exact same instance
* imports can be either via constructors or through properties. The export provider supports both
* the export provider itself includes logging statements, but it must do so with an explicitly created `ILog` instance. Obviously the export provider cannot import an `ILoggerService` itself or we'd have a chicken and egg problem!

We can tell MEF to use our custom `ExportProvider` in the usual fashion:

```csharp
var compositionContainer = new CompositionContainer(new LoggerServiceExportProvider());
```

With this infrastructure in place, we achieve our objectives entirely. The code I included right at the beginning of this post will work, and any log entries will include the details of the originating type. And it is ridiculously easy for us to imbue components created by MEF with logging statements. Simply add the import and then invoke the simple-to-use APIs.

[Here is a working example]({{ page.assets }}LogExportProvider.zip). Enjoy!