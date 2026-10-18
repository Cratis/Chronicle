// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
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
        var buffer = new ArrayBufferWriter<byte>();
        buffer.Write(Encoding.UTF8.GetBytes($"{eventTypeId.Value}|{eventSourceId.Value}|"));
        CanonicalJson.Write(content, buffer);
        var hashBytes = SHA256.HashData(buffer.WrittenSpan);
        return Convert.ToBase64String(hashBytes);
    }
}
