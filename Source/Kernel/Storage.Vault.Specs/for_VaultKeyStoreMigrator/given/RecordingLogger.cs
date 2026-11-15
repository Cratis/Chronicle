// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;

/// <summary>
/// Keeps every message, and every exception attached to one, as text.
/// </summary>
/// <typeparam name="T">The category.</typeparam>
public class RecordingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
        if (exception is not null)
        {
            Messages.Add(exception.ToString());
        }

        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            Messages.AddRange(values.Select(value => $"{value.Value}"));
        }
    }
}
