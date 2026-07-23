// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.given;

/// <summary>
/// A recording <see cref="ILogger{TCategoryName}"/> test double that captures the levels it was asked to log at.
/// </summary>
/// <typeparam name="T">The category type the logger is for.</typeparam>
/// <remarks>
/// Used because <c>ILogger&lt;Jobs&gt;</c> cannot be proxied by NSubstitute (the <c>Jobs</c> type is internal in a
/// strong-named assembly), so a hand-written double is needed to verify that a failure gets logged.
/// </remarks>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    readonly List<LogLevel> _entries = [];

    /// <summary>
    /// Gets the log levels captured, in the order they were logged.
    /// </summary>
    public IReadOnlyList<LogLevel> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add(logLevel);
}
