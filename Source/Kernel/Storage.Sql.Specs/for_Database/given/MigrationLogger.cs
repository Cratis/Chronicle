// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class MigrationLogger : ILogger<Database>
{
    public List<Dictionary<string, object?>> Entries { get; } = [];

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Snapshot while logging: generated log state can be pooled and cleared on return.
        Entries.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary());
    }
}
