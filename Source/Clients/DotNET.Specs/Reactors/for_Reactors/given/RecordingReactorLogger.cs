// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

public class RecordingReactorLogger : ILogger<Reactors>
{
    public ConcurrentQueue<IReadOnlyDictionary<string, object?>> States { get; } = new();
    public TaskCompletionSource ReRegistered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (eventId.Id is 8100 or 8101 && state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            // Generated logging state is pooled and cleared after Log returns; snapshot it here.
            States.Enqueue(properties.ToDictionary(pair => pair.Key, pair => pair.Value));
            if (States.Count(entry => entry.TryGetValue("Outcome", out var outcome) && Equals(outcome, "stream-started")) == 2)
            {
                ReRegistered.TrySetResult();
            }
        }
    }
}
