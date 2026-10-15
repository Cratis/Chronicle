// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_worker_died_before_completing_the_occurrence : given.an_event_sequence_with_effect_receipts
{
    OccurrenceDelivery _queue;
    Storage.InMemory.Occurrences.OccurrencesStorage _occurrences;
    TimeProvider _clock;
    OccurrenceId _id;
    OccurrenceLeaseResult _completed;

    async Task Establish()
    {
        _clock = Substitute.For<TimeProvider>();
        _clock.GetUtcNow().Returns(DateTimeOffset.UnixEpoch);
        _occurrences = new();
        var definitions = new Storage.InMemory.Occurrences.TriggerDefinitionsStorage();
        await definitions.TrySave(new(new("trigger"), "Trigger", "{\"type\":\"object\"}", 1), null);
        _queue = new(_occurrences, definitions, new("trigger"), new(), _clock);
        _id = new((await _queue.Record("effect", "{}", OccurrenceKind.Application)).Id);
        await _queue.Claim("killed-worker", 1, 1);
        _receipt = new(_id.Value, "command", 1, "{\"answer\":42}");
        await AppendEffect();

        // No Complete: the worker disappears after its effect committed.
        _clock.GetUtcNow().Returns(DateTimeOffset.UnixEpoch.AddSeconds(1));
    }

    async Task Because()
    {
        var claim = (await _queue.Claim("replacement", 0, 1)).Single();
        _receipt = _receipt with { Epoch = claim.Epoch };
        _result = await AppendEffect();
        _completed = await _queue.Complete(new(claim.Id, claim.Epoch));
    }

    [Fact] void should_find_the_first_workers_receipt() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_recover_the_recorded_response() => _result.EffectReceipt!.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_leave_exactly_one_set_of_events() => _durableStorage.Events.Count.ShouldEqual(1);
    [Fact] void should_allow_the_replacement_to_complete() => _completed.ShouldEqual(OccurrenceLeaseResult.Updated);
    [Fact] async Task should_finish_at_the_new_epoch() => (await _occurrences.Get(_id))!.Epoch.ShouldEqual(2);
}
