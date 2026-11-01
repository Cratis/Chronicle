// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_invalid_compact_fields : given.a_compact_serializer
{
    Exception?[] _errors;

    void Because()
    {
        byte[][] messages = [
            CompactMessage(2, [1, 0, 0, 0, 0]),
            CompactMessage(1, null),
            CompactMessage(1, [255]),
            CompactMessage(1, [1, 255, 255, 255, 255]),
            CompactMessage(1, [1, 1, 0, 0, 0, 1, 0, 0, 0, 97]),
            CompactMessage(1, [1, 0, 0, 0, 0, 255]),
            CompactMessage(1, Enumerable.Range(0, 66).SelectMany(_ => new byte[] { 1, 1, 0, 0, 0, 1, 0, 0, 0, 97 }).Concat(new byte[] { 0 }).ToArray())];
        _errors = messages.Select(bytes => Record.Exception(() => serializer.Deserialize(bytes))).ToArray();
    }

    [Fact] void should_reject_unknown_versions() => _errors[0].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_require_content() => _errors[1].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_unknown_node_tags() => _errors[2].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_bound_counts_by_the_payload() => _errors[3].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_truncated_nodes() => _errors[4].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_trailing_content() => _errors[5].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_bound_nesting() => _errors[6].ShouldBeOfExactType<InvalidDataException>();
}
