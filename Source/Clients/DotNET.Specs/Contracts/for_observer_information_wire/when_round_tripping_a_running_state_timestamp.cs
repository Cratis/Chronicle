// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Contracts.for_observer_information_wire;

public class when_round_tripping_a_running_state_timestamp : Specification
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverInformation _present;
    ObserverInformation _absent;
    OldObserverInformation _old;
    string _schema;

    void Because()
    {
        using var payload = new MemoryStream();
        Serializer.Serialize(payload, new ObserverInformation { Id = "observer", RunningStateChangedAt = _timestamp });
        payload.Position = 0;
        _present = Serializer.Deserialize<ObserverInformation>(payload);
        payload.Position = 0;
        var oldModel = RuntimeTypeModel.Create();
        oldModel.Add(typeof(OldObserverInformation), applyDefaultBehaviour: false).Add(1, nameof(OldObserverInformation.Id));
        _old = oldModel.Deserialize<OldObserverInformation>(payload);
        using var legacy = new MemoryStream();
        oldModel.Serialize(legacy, new OldObserverInformation { Id = "legacy" });
        legacy.Position = 0;
        _absent = Serializer.Deserialize<ObserverInformation>(legacy);
        _schema = RuntimeTypeModel.Default.GetSchema(typeof(ObserverInformation), ProtoSyntax.Proto3);
    }

    [Fact] void should_keep_the_timestamp() => _present.RunningStateChangedAt.ShouldEqual(_timestamp);
    [Fact] void should_leave_an_absent_timestamp_unknown() => _absent.RunningStateChangedAt.ShouldBeNull();
    [Fact] void should_allow_old_readers_to_ignore_the_timestamp() => _old.Id.ShouldEqual("observer");
    [Fact] void should_describe_field_thirteen() => _schema.ShouldContain("RunningStateChangedAt = 13;");

    class OldObserverInformation
    {
        public string Id { get; set; }
    }
}
