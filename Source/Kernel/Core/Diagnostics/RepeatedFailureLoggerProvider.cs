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
/// <para>
/// The counts are reported at the level of the highest failure counted, under the category of <see cref="RepeatedFailureLog"/>.
/// They are written straight to the wrapped provider, so the filter rules of the logger factory (such as the Logging:LogLevel
/// configuration) do not apply to that category; only the wrapped provider's own filtering does. The counts still running when
/// the provider is disposed, or when the process exits, are reported then rather than lost.
/// </para>
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
        AppDomain.CurrentDomain.ProcessExit += FlushAtExit;
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
        AppDomain.CurrentDomain.ProcessExit -= FlushAtExit;
        _flush.Dispose();
        Flush(includeRunningWindows: true);
        _inner.Dispose();
    }

    /// <summary>
    /// Gets the message template of a log entry, telling different failures in the same category apart.
    /// </summary>
    /// <remarks>
    /// The template is what the summary shows, so it is preferred over the event name. A state that is a struct is boxed when the
    /// template is read through the interface; that only happens for entries at warning or above that carry an exception, which have
    /// already paid for the exception and its stack trace.
    /// </remarks>
    /// <typeparam name="TState">The type of the state of the entry.</typeparam>
    /// <param name="state">The state of the entry.</param>
    /// <param name="eventId">The <see cref="EventId"/> of the entry, whose name is used when the state has no template.</param>
    /// <returns>The message template.</returns>
    internal static string MessageTemplateOf<TState>(TState state, EventId eventId)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> { Count: > 0 } values &&
            string.Equals(values[^1].Key, "{OriginalFormat}", StringComparison.Ordinal) &&
            values[^1].Value is string template)
        {
            return template;
        }

        return eventId.Name ?? string.Empty;
    }

    /// <summary>
    /// Writes the counts of the failures that were not written in windows that have ended.
    /// </summary>
    /// <param name="includeRunningWindows">Whether to also write the counts of windows that are still running, for when nothing will write them later.</param>
    internal void Flush(bool includeRunningWindows = false)
    {
        var seconds = (int)_log.Window.TotalSeconds;
        foreach (var failures in _log.TakeEnded(includeRunningWindows))
        {
            if (failures.IsUntracked)
            {
                UntrackedSuppressed(_summary, failures.Level, failures.Suppressed, failures.Exception.Name, seconds);
            }
            else
            {
                Suppressed(_summary, failures.Level, failures.Suppressed, failures.Exception.Name, failures.Category, failures.Message, seconds);
            }
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

    [LoggerMessage("{Suppressed} more {Exception} failures from {Category} ('{Message}') were counted but not written; the first in each {Seconds} second window is written in full")]
    static partial void Suppressed(ILogger logger, LogLevel level, int suppressed, string exception, string category, string message, int seconds);

    [LoggerMessage("{Suppressed} more {Exception} failures were counted but not written; too many kinds of failures were failing at once to track them separately, so only the first few of them in each {Seconds} second window were written in full")]
    static partial void UntrackedSuppressed(ILogger logger, LogLevel level, int suppressed, string exception, int seconds);

    void FlushAtExit(object? sender, EventArgs args)
    {
        try
        {
            Flush(includeRunningWindows: true);
        }
        catch (Exception failure)
        {
            // The process is exiting and the wrapped provider may already be gone; the console is all that is left.
            Console.Error.WriteLine(failure);
        }
    }

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
