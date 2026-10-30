// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Patches;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveUnhashedUniqueConstraintValues.given;

/// <summary>
/// Records the messages the cleanup patch logs, formatted while the call is made.
/// </summary>
public sealed class RecordingCleanupLogger : ILogger<RemoveUnhashedUniqueConstraintValues>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    /// <summary>
    /// Gets the formatted messages that were logged.
    /// </summary>
    public IEnumerable<string> Messages => _entries.Select(_ => _.Message);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue((logLevel, formatter(state, exception)));
}
