// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Contracts.for_observer_information_wire;

public class when_round_tripping_a_running_state_timestamp : given.an_observer_information_payload
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, 123, TimeSpan.FromHours(2));
    ObserverInformation _result;
    string _schema;

    void Because()
    {
        _result = Deserialize<ObserverInformation>(Serialize(CreateInformation(_timestamp)));
        _schema = RuntimeTypeModel.Default.GetSchema(typeof(ObserverInformation), ProtoSyntax.Proto3);
    }

    [Fact] void should_keep_the_timestamp() => ((DateTimeOffset?)_result.RunningStateChangedAt).ShouldEqual(_timestamp);
    [Fact] void should_keep_the_offset() => ((DateTimeOffset)_result.RunningStateChangedAt).Offset.ShouldEqual(_timestamp.Offset);
    [Fact] void should_use_the_existing_primitive_at_field_thirteen() => _schema.ShouldContain("SerializableDateTimeOffset RunningStateChangedAt = 13;");
    [Fact] void should_describe_the_primitive_value() => _schema.ShouldContain("string Value = 1;");
}
