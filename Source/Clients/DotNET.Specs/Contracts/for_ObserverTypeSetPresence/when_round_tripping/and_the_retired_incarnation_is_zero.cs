// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.TypeSets;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_ObserverTypeSetPresence.when_round_tripping;

public class and_the_retired_incarnation_is_zero : Specification
{
    ObserverEventTypeRetirementResponse _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ObserverEventTypeRetirementResponse { EventTypeId = "event", Incarnation = new() { Value = 0 } });
        stream.Position = 0;
        _result = Serializer.Deserialize<ObserverEventTypeRetirementResponse>(stream);
    }

    [Fact] void should_keep_the_incarnation_present() => _result.Incarnation.ShouldNotBeNull();
    [Fact] void should_keep_the_incarnation_value_zero() => _result.Incarnation.Value.ShouldEqual(0L);
}
