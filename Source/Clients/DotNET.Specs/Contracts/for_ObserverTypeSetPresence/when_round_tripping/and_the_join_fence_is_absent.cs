// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.TypeSets;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_ObserverTypeSetPresence.when_round_tripping;

public class and_the_join_fence_is_absent : Specification
{
    ObserverTypeSetMember _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ObserverTypeSetMember { EventTypeId = "event", JoinFence = null });
        stream.Position = 0;
        _result = Serializer.Deserialize<ObserverTypeSetMember>(stream);
    }

    [Fact] void should_keep_the_fence_absent() => _result.JoinFence.ShouldBeNull();
}
