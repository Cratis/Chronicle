// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventHashCalculator"/>.
/// </summary>
[Singleton]
public class EventHashCalculator : IEventHashCalculator
{
    /// <inheritdoc/>
    public EventHash Calculate(EventTypeId eventTypeId, EventSourceId eventSourceId, ExpandoObject content)
    {
        using var buffer = new CanonicalJsonBuffer();
        WritePrefixPart(eventTypeId.Value, buffer);
        WritePrefixPart(eventSourceId.Value, buffer);
        CanonicalJson.Write(content, buffer);
        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(buffer.WrittenSpan, hashBytes);
        return Convert.ToBase64String(hashBytes);
    }

    static void WritePrefixPart(string? value, CanonicalJsonBuffer buffer)
    {
        var destination = buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(value?.Length ?? 0) + 1);
        var written = Encoding.UTF8.GetBytes(value.AsSpan(), destination);
        destination[written] = (byte)'|';
        buffer.Advance(written + 1);
    }
}
