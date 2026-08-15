// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;

/// <summary>
/// A recording <see cref="ILogger{TCategoryName}"/> test double that captures the level and the rendered message of
/// everything it was asked to log.
/// </summary>
/// <typeparam name="T">The category type the logger is for.</typeparam>
/// <remarks>
/// The rendered message is what an operator reads and what a log aggregator stores, so it is what the specs assert
/// on - both for what an erasure has to record and for what it must never contain.
/// </remarks>
public sealed class RecordingLogger<T> : ILogger<T>
{
    readonly List<(LogLevel Level, string Message)> _entries = [];

    /// <summary>
    /// Gets the entries captured, in the order they were logged.
    /// </summary>
    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    /// <summary>
    /// Gets every rendered message captured.
    /// </summary>
    public IEnumerable<string> Messages => _entries.Select(_ => _.Message);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, formatter(state, exception)));
}
