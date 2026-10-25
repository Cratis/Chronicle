// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard.given;

public sealed class SchemaLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, Dictionary<string, object?> State)> Entries { get; } = [];

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Generated log state can be pooled and cleared after Log returns.
        Entries.Add((logLevel, ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary()));
    }
}
