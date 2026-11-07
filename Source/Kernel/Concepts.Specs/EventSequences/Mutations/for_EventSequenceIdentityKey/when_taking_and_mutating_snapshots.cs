// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceIdentityKey;

public class when_taking_and_mutating_snapshots : Specification
{
    byte[] _source;
    EventSequenceIdentityKey _key;
    byte[] _firstSnapshot;
    byte[] _secondSnapshot;

    void Establish()
    {
        _source = [1, 2, 3];
        _key = new(_source);
    }

    void Because()
    {
        _source[0] = 9;
        _firstSnapshot = _key.Snapshot();
        _firstSnapshot[1] = 9;
        _secondSnapshot = _key.Snapshot();
    }

    [Fact] void should_defensively_copy_the_source() => _secondSnapshot[0].ShouldEqual((byte)1);
    [Fact] void should_return_a_new_snapshot() => ReferenceEquals(_firstSnapshot, _secondSnapshot).ShouldBeFalse();
    [Fact] void should_protect_content_from_snapshot_mutation() => _secondSnapshot[1].ShouldEqual((byte)2);
}
