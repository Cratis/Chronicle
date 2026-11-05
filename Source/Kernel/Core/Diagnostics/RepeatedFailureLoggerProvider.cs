// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Wraps a <see cref="ILoggerProvider"/> so that a repeated failure is written a few times and then counted (see <see cref="RepeatedFailureLog"/>).
/// </summary>
public sealed partial class RepeatedFailureLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    readonly ILoggerProvider _inner;
    readonly RepeatedFailureLog _log;
    readonly Timer _flush;
    readonly ILogger _summary;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatedFailureLoggerProvider"/> class.
    /// </summary>
    /// <param name="inner">The provider that writes.</param>
    /// <param name="log">The <see cref="RepeatedFailureLog"/> deciding what to write.</param>
    public RepeatedFailureLoggerProvider(ILoggerProvider inner, RepeatedFailureLog log)
    {
        _inner = inner;
        _log = log;
        _summary = inner.CreateLogger(typeof(RepeatedFailureLog).FullName!);
        _flush = new(_ => Flush(), null, RepeatedFailureLog.DefaultWindow, RepeatedFailureLog.DefaultWindow);
    }

    /// <summary>
    /// Wraps every logger provider in the collection, so the failures of all of them are bounded.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> holding the providers.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection ThrottleRepeatedFailures(IServiceCollection services)
    {
        var log = new RepeatedFailureLog(RepeatedFailureLog.DefaultWindow, RepeatedFailureLog.DefaultAllowedPerWindow, TimeProvider.System);
        foreach (var descriptor in services.Where(_ => _.ServiceType == typeof(ILoggerProvider) && _.ServiceKey is null).ToArray())
        {
            services[services.IndexOf(descriptor)] = ServiceDescriptor.Singleton<ILoggerProvider>(serviceProvider => new RepeatedFailureLoggerProvider(Create(serviceProvider, descriptor), log));
        }

        return services;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new Logger(_inner.CreateLogger(categoryName), categoryName, _log);

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        if (_inner is ISupportExternalScope supports)
        {
            supports.SetScopeProvider(scopeProvider);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _flush.Dispose();
        Flush();
        _inner.Dispose();
    }

    static ILoggerProvider Create(IServiceProvider serviceProvider, ServiceDescriptor descriptor) =>
        descriptor switch
        {
            { ImplementationInstance: ILoggerProvider instance } => instance,
            { ImplementationFactory: { } factory } => (ILoggerProvider)factory(serviceProvider),
            { ImplementationType: { } type } => (ILoggerProvider)ActivatorUtilities.CreateInstance(serviceProvider, type),
            _ => throw new InvalidOperationException("Unsupported logger provider registration")
        };

    [LoggerMessage(LogLevel.Warning, "{Suppressed} more {Exception} failures from {Category} were not written in the last {Seconds} seconds")]
    static partial void Suppressed(ILogger logger, int suppressed, string exception, string category, int seconds);

    void Flush()
    {
        foreach (var (category, exception, suppressed) in _log.TakeEnded())
        {
            Suppressed(_summary, suppressed, exception.Name, category, (int)RepeatedFailureLog.DefaultWindow.TotalSeconds);
        }
    }

    sealed class Logger(ILogger inner, string category, RepeatedFailureLog log) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (log.ShouldWrite(category, logLevel, exception))
            {
                inner.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
