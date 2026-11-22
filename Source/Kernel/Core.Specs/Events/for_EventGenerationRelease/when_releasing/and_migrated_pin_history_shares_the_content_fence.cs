// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_migrated_pin_history_shares_the_content_fence : given.a_fenced_history_release
{
    void Establish() => _event = _event with
    {
        GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}" },
        Revisions = [new(1, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred, "{\"name\":\"ciphertext\"}")]
    };

    async Task Because() => await ReleaseEvent();

    [Fact] void should_deliver_migrated_plaintext() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_release_all_history() => HistoryIsReleased().ShouldBeTrue();
    [Fact] void should_release_source_and_three_history_payloads() => _releaseCalls.ShouldEqual(4);
    [Fact] void should_share_one_fence_between_strict_source_release_and_history() => _fenceReadsDuringRelease.ShouldEqual(1);
    [Fact] void should_read_a_fresh_fence_after_delivery() => _fenceReads.ShouldEqual(2);
    [Fact] async Task should_never_release_migrated_plaintext_again() => await _metadata.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _schema, "person", Arg.Any<JsonObject>());
}
