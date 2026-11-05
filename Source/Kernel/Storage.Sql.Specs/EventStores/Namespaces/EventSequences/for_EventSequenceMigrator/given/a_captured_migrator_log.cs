// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>
/// Records the level and the rendered text of every message the event sequence migrator logs.
/// </summary>
public sealed class a_captured_migrator_log : ILogger<EventSequenceMigrator>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message)> _messages = new();

    public IReadOnlyList<(LogLevel Level, string Message)> Messages => [.. _messages];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _messages.Enqueue((logLevel, formatter(state, exception)));
}
