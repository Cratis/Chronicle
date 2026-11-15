// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.given;

/// <summary>
/// A complete application log entry, including structured state and exception.
/// </summary>
/// <param name="Level">The level.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="State">The original structured values.</param>
/// <param name="Error">The original exception.</param>
public record RecordedLogEntry(LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object?>> State, Exception? Error)
{
    /// <summary>
    /// Gets all observable diagnostic text.
    /// </summary>
    public string Text => string.Join("\n", Message, string.Join('\n', State.Select(value => $"{value.Key}={value.Value}")), Error?.ToString());
}
