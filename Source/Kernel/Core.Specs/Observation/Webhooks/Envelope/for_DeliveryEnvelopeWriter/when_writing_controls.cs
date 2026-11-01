// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_DeliveryEnvelopeWriter;

public class when_writing_controls : given.a_delivery_request
{
    JsonElement _started;
    JsonElement _ended;
    JsonElement _ping;
    JsonElement _reducer;

    void Because()
    {
        _started = Write(DeliveryKind.ReplayStarted);
        _ended = Write(DeliveryKind.ReplayEnded);
        _ping = Write(DeliveryKind.Ping);
        var envelope = _writer.ForEvents(_request with { ObserverType = "reducer" });
        _reducer = JsonDocument.Parse(_writer.Serialize(envelope with { Batches = [envelope.Batches[0] with { InitialState = JsonNode.Parse("{\"count\":1}") }] })).RootElement;
    }

    [Fact] void should_match_the_ping_snapshot() => _ping.GetRawText().ShouldEqual("""{"kind":"ping","observerType":"reactor","observerId":"observer","eventStore":"store","namespace":"namespace","eventSequenceId":"sequence","deliveryId":"msg_a284ef239a9ab70ad2713e55aff02bb2eea6b983abcc2dcdd59f8546b0a89758","batches":[],"replay":false,"replayEpoch":null}""");
    [Fact] void should_match_the_reserved_start_snapshot() => _started.GetRawText().ShouldEqual("""{"kind":"replay-started","observerType":"reactor","observerId":"observer","eventStore":"store","namespace":"namespace","eventSequenceId":"sequence","deliveryId":"msg_8a7c5b77ba2ae8f75000409e32a0fdc75fc6bc4a16bb99846773182878e4257c","batches":[],"replay":false,"replayEpoch":null}""");
    [Fact] void should_match_the_reserved_end_snapshot() => _ended.GetRawText().ShouldEqual("""{"kind":"replay-ended","observerType":"reactor","observerId":"observer","eventStore":"store","namespace":"namespace","eventSequenceId":"sequence","deliveryId":"msg_5f272191ac4f939d2160c777fc731be929fd264aa06b423fd12db54148ba429a","batches":[],"replay":false,"replayEpoch":null}""");
    [Fact] void should_write_the_reserved_start_wire_name() => _started.GetProperty("kind").GetString().ShouldEqual("replay-started");
    [Fact] void should_write_the_reserved_end_wire_name() => _ended.GetProperty("kind").GetString().ShouldEqual("replay-ended");
    [Fact] void should_write_the_ping_wire_name() => _ping.GetProperty("kind").GetString().ShouldEqual("ping");
    [Fact] void should_not_promote_reserved_controls_to_replay_support() => _started.GetProperty("replay").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_attach_events_to_controls() => _started.GetProperty("batches").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_attach_events_to_ping() => _ping.GetProperty("batches").GetArrayLength().ShouldEqual(0);
    [Fact] void should_distinguish_control_identity() => _started.GetProperty("deliveryId").GetString().ShouldNotEqual(_ended.GetProperty("deliveryId").GetString());
    [Fact] void should_write_reducer_type() => _reducer.GetProperty("observerType").GetString().ShouldEqual("reducer");
    [Fact] void should_write_reducer_initial_state() => _reducer.GetProperty("batches")[0].GetProperty("initialState").GetRawText().ShouldEqual("{\"count\":1}");

    JsonElement Write(DeliveryKind kind) => JsonDocument.Parse(_writer.Serialize(_writer.Control(kind, _request))).RootElement;
}
