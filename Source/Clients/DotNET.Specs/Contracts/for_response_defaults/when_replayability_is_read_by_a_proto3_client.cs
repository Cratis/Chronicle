// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Contracts.for_response_defaults;

public class when_replayability_is_read_by_a_proto3_client : Specification
{
    byte[] _replayablePayload;
    byte[] _notReplayablePayload;
    bool _replayable;
    bool _notReplayable;

    void Because()
    {
        _replayablePayload = Serialize(true);
        _notReplayablePayload = Serialize(false);
        using var replayableStream = new MemoryStream(_replayablePayload);
        using var notReplayableStream = new MemoryStream(_notReplayablePayload);
        var proto3Model = RuntimeTypeModel.Create();
        proto3Model.Add(typeof(Proto3ObserverInformation), applyDefaultBehaviour: false).Add(10, nameof(Proto3ObserverInformation.IsReplayable));
        _replayable = proto3Model.Deserialize<Proto3ObserverInformation>(replayableStream).IsReplayable;
        _notReplayable = proto3Model.Deserialize<Proto3ObserverInformation>(notReplayableStream).IsReplayable;
    }

    [Fact] void should_write_true_at_field_ten() => _replayablePayload.ShouldEqual([0x50, 0x01]);
    [Fact] void should_write_false_at_field_ten() => _notReplayablePayload.ShouldEqual([0x50, 0x00]);
    [Fact] void should_decode_replayable_as_true() => _replayable.ShouldBeTrue();
    [Fact] void should_decode_not_replayable_as_false() => _notReplayable.ShouldBeFalse();

    static byte[] Serialize(bool isReplayable)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ObserverInformation { IsReplayable = isReplayable });
        return stream.ToArray();
    }

    /// <summary>
    /// Models a proto3 client, whose missing boolean starts at false irrespective of C# initializers.
    /// </summary>
    class Proto3ObserverInformation
    {
        public bool IsReplayable { get; set; }
    }
}
