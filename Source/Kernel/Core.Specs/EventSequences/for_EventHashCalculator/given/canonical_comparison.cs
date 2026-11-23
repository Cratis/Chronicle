// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

public record canonical_comparison(string Name, bool ExpectedDirect, bool Direct, bool BytesEqual, bool HashEqual)
{
    static readonly EventTypeId _eventType = new("differential<>&");
    static readonly EventSourceId _eventSource = new("source|é🚀");

    public static canonical_comparison For(string name, ExpandoObject content, bool expectedDirect)
    {
        var expectedBytes = stage_a_canonical_oracle.Encode(content);
        var expectedHash = stage_a_canonical_oracle.Hash(_eventType, _eventSource, expectedBytes);
        var direct = CanonicalJsonValues.CanWrite(content);
        var actualBytes = new ArrayBufferWriter<byte>();
        CanonicalJson.Write(content, actualBytes);
        var actualHash = new EventHashCalculator().Calculate(_eventType, _eventSource, content).Value;
        return new(name, expectedDirect, direct, actualBytes.WrittenSpan.SequenceEqual(expectedBytes), actualHash == expectedHash);
    }
}
