// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Logging;

/// <summary>
/// Hash functions for message templates. See <see cref="Compute"/>.
/// </summary>
/// <remarks>
/// Based on and adapted from https://github.com/serilog/serilog-formatting-compact.
/// </remarks>
public static class EventIdHash
{
    /// <summary>
    /// Compute a 32-bit hash of the provided <paramref name="messageTemplate"/>. The
    /// resulting hash value can be uses as an event id in lieu of transmitting the
    /// full template string.
    /// </summary>
    /// <param name="messageTemplate">A message template.</param>
    /// <returns>A 32-bit hash of the template.</returns>
    public static uint Compute(string messageTemplate)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate, nameof(messageTemplate));

        // Jenkins one-at-a-time https://en.wikipedia.org/wiki/Jenkins_hash_function
        unchecked
        {
            uint hash = 0;
            for (var i = 0; i < messageTemplate.Length; ++i)
            {
                hash += messageTemplate[i];
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }
    }
}
