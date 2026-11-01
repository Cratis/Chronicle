// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.TypeSets;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_ObserverTypeSetPresence.when_round_tripping;

public class and_the_retired_incarnation_is_absent : Specification
{
    ObserverEventTypeRetirementResponse _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ObserverEventTypeRetirementResponse { EventTypeId = "event", Incarnation = null });
        stream.Position = 0;
        _result = Serializer.Deserialize<ObserverEventTypeRetirementResponse>(stream);
    }

    [Fact] void should_keep_the_incarnation_absent() => _result.Incarnation.ShouldBeNull();
}
