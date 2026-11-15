// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKeyIdBytes;

public class when_converting_a_canonical_id : Specification
{
    const string Id = "00ff10a5b6c7d8e9f0123456789abcde";
    byte[] _bytes;
    UniqueConstraintStoreKeyId _roundTripped;

    void Because()
    {
        _bytes = UniqueConstraintStoreKeyIdBytes.From(new UniqueConstraintStoreKeyId(Id));
        _roundTripped = UniqueConstraintStoreKeyIdBytes.To(_bytes);
    }

    [Fact] void should_produce_sixteen_bytes() => _bytes.Length.ShouldEqual(16);
    [Fact] void should_round_trip_to_the_same_id() => _roundTripped.Value.ShouldEqual(Id);
}
