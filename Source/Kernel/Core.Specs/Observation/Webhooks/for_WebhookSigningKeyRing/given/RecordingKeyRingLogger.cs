// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.given;

/// <summary>
/// Records formatted messages and structured values to check that neither contains key material.
/// </summary>
public sealed class RecordingKeyRingLogger : ILogger<WebhookSigningKeyRing>
{
    /// <summary>
    /// Gets all recorded messages and structured values.
    /// </summary>
    public List<string> Entries { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(formatter(state, exception));
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            Entries.AddRange(values.Select(value => $"{value.Key}={value.Value}"));
        }
    }
}
