// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Helpers for the applied event markers a sink keeps on a document for <see cref="SinkWriteMode.OncePerSourceEvent"/>.
/// </summary>
/// <remarks>
/// A marker slot is <c language="text">{ s, n }</c>: <c language="text">s</c> identifies the event stream that made the write and <c language="text">n</c> is the event
/// sequence number of the event that made it. A write is a no-op when its slot already holds the same stream with a
/// sequence number at or beyond the event's.
/// </remarks>
public static class AppliedEventMarkers
{
    const char Separator = '\u001f';

    /// <summary>
    /// Gets the identity of an event stream, scoped to the namespace.
    /// </summary>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="value">The value identifying the stream: an event source id, or a join key.</param>
    /// <returns>A fixed length, url safe string.</returns>
    public static string StreamHash(string @namespace, string value) => Hash($"{@namespace}{Separator}{value}", 16);

    /// <summary>
    /// Gets the identity of a root join definition.
    /// </summary>
    /// <param name="eventTypeId">The id of the event type that joins.</param>
    /// <param name="onPath">The property path the join is on.</param>
    /// <returns>A fixed length, url safe string.</returns>
    public static string DefinitionId(string eventTypeId, string onPath) => Hash($"{eventTypeId}|{onPath}", 8);

    static string Hash(string value, int length)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(hash, 0, length).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
