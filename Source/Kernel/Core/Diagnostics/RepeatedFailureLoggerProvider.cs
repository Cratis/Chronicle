// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Wraps a <see cref="ILoggerProvider"/> so that a repeated failure is written once in each window and then counted (see <see cref="RepeatedFailureLog"/>).
/// </summary>
/// <remarks>
/// Every wrapped provider decides on its own, so each of them writes the first occurrence of a failure and reports its own count.
/// </remarks>
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
        _flush = new(_ => Flush(), null, log.Window, log.Window);
    }

    /// <summary>
    /// Wraps every logger provider in the collection, so the failures of all of them are bounded.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> holding the providers.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection ThrottleRepeatedFailures(IServiceCollection services)
    {
        foreach (var descriptor in services.Where(_ => _.ServiceType == typeof(ILoggerProvider) && _.ServiceKey is null).ToArray())
        {
            services[services.IndexOf(descriptor)] = ServiceDescriptor.Singleton<ILoggerProvider>(serviceProvider => new RepeatedFailureLoggerProvider(
                Create(serviceProvider, descriptor),
                new RepeatedFailureLog(RepeatedFailureLog.DefaultWindow, RepeatedFailureLog.DefaultAllowedPerWindow, TimeProvider.System)));
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

    /// <summary>
    /// Writes the counts of the failures that were not written in windows that have ended.
    /// </summary>
    internal void Flush()
    {
        foreach (var (category, message, exception, suppressed) in _log.TakeEnded())
        {
            Suppressed(_summary, suppressed, exception.Name, category, message, (int)_log.Window.TotalSeconds);
        }
    }

    static ILoggerProvider Create(IServiceProvider serviceProvider, ServiceDescriptor descriptor) =>
        descriptor switch
        {
            { ImplementationInstance: ILoggerProvider instance } => instance,
            { ImplementationFactory: { } factory } => (ILoggerProvider)factory(serviceProvider),
            { ImplementationType: { } type } => (ILoggerProvider)ActivatorUtilities.CreateInstance(serviceProvider, type),
            _ => throw new InvalidOperationException("Unsupported logger provider registration")
        };

    static string MessageTemplateOf<TState>(TState state, EventId eventId)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> { Count: > 0 } values &&
            string.Equals(values[^1].Key, "{OriginalFormat}", StringComparison.Ordinal) &&
            values[^1].Value is string template)
        {
            return template;
        }

        return eventId.Name ?? string.Empty;
    }

    [LoggerMessage(LogLevel.Warning, "{Suppressed} more {Exception} failures from {Category} ('{Message}') were counted but not written; the first in each {Seconds} second window is written in full")]
    static partial void Suppressed(ILogger logger, int suppressed, string exception, string category, string message, int seconds);

    sealed class Logger(ILogger inner, string category, RepeatedFailureLog log) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is null || logLevel < LogLevel.Warning || log.ShouldWrite(category, MessageTemplateOf(state, eventId), logLevel, exception))
            {
                inner.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
