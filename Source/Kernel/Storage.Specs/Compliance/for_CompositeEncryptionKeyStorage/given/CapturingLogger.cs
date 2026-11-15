// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.given;

public sealed class CapturingLogger<T> : ILogger<T>
{
    readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs ? string.Join(' ', pairs.Select(_ => $"{_.Key}={_.Value}")) : string.Empty;
        _lines.Add(formatter(state, exception) + " " + properties + " " + exception);
    }
}
