// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_parts_are_supported_scalars : Specification
{
    string _result;

    void Because() => _result = CompositeKeyEncoding.Encode(new Dictionary<string, object?>
    {
        ["a"] = (byte)5,
        ["b"] = (ushort)6,
        ["c"] = new DateOnly(2026, 10, 23),
        ["d"] = new TimeOnly(8, 30)
    });

    [Fact] void should_encode_them() => _result.ShouldEqual("~k1:1:51:610:2026-10-2316:08:30:00.0000000");
}
