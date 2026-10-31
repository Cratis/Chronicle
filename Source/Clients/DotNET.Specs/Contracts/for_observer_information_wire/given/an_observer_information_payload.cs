// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_observer_information_wire.given;

public class an_observer_information_payload : Specification
{
    protected static ObserverInformation CreateInformation(DateTimeOffset? timestamp = null) => new()
    {
        Id = "observer",
        EventSequenceId = "event-log",
        Type = ObserverType.Reactor,
        Owner = ObserverOwner.Client,
        EventTypes = [new EventType { Id = "event", Generation = 1 }],
        NextEventSequenceNumber = 42,
        LastHandledEventSequenceNumber = 41,
        RunningState = ObserverRunningState.Disconnected,
        IsSubscribed = true,
        IsReplayable = true,
        TailEventSequenceNumber = 99,
        HandledEventCount = 17,
        RunningStateChangedAt = timestamp
    };

    protected static OldObserverInformation CreateOldInformation() => new()
    {
        Id = "observer",
        EventSequenceId = "event-log",
        Type = ObserverType.Reactor,
        Owner = ObserverOwner.Client,
        EventTypes = [new EventType { Id = "event", Generation = 1 }],
        NextEventSequenceNumber = 42,
        LastHandledEventSequenceNumber = 41,
        RunningState = ObserverRunningState.Disconnected,
        IsSubscribed = true,
        IsReplayable = true,
        TailEventSequenceNumber = 99,
        HandledEventCount = 17
    };

    protected static byte[] Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, value);
        return stream.ToArray();
    }

    protected static T Deserialize<T>(byte[] payload)
    {
        using var stream = new MemoryStream(payload);
        return Serializer.Deserialize<T>(stream);
    }

    [ProtoContract]
    protected class OldObserverInformation
    {
        [ProtoMember(1)] public string Id { get; set; }
        [ProtoMember(2)] public string EventSequenceId { get; set; }
        [ProtoMember(3)] public ObserverType Type { get; set; }
        [ProtoMember(4)] public ObserverOwner Owner { get; set; }
        [ProtoMember(5)] public IEnumerable<EventType> EventTypes { get; set; } = new List<EventType>();
        [ProtoMember(6)] public ulong NextEventSequenceNumber { get; set; }
        [ProtoMember(7)] public ulong LastHandledEventSequenceNumber { get; set; }
        [ProtoMember(8)] public ObserverRunningState RunningState { get; set; }
        [ProtoMember(9)] public bool IsSubscribed { get; set; }
        [ProtoMember(10, IsRequired = true)]
        [DefaultValue(true)]
        public bool IsReplayable { get; set; } = true;
        [ProtoMember(11)] public ulong TailEventSequenceNumber { get; set; }
        [ProtoMember(12)] public ulong HandledEventCount { get; set; }
    }
}
