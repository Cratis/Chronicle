// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_Projections.when_getting_external_event_store_subscriptions;

public class and_cross_generation_sources_are_combined : Events.for_EventTypes.given.a_cross_generation_registry
{
    Projections _subject;
    Dictionary<string, EventTypeId[]> _contributions;

    async Task Establish() => _subject = await CreateCombinedProjections();

    async Task Because()
    {
        await _subject.Discover();
        _contributions = _subject.GetExternalEventStoreSubscriptions()
            .ToDictionary(subscription => subscription.EventStoreName, subscription => subscription.EventTypeIds.ToArray());
    }

    [Fact] void should_discover_every_real_projection_definition() => _subject.Definitions.Count().ShouldEqual(9);
    [Fact] void should_keep_duplicate_current_source_definitions() => _subject.Definitions.Count(definition => definition.EventSequenceId == "inbox-current-owner").ShouldEqual(4);
    [Fact] void should_keep_both_custom_sequence_definitions_without_subscribing_to_them() => _subject.Definitions.Count(definition => definition.EventSequenceId == CustomSequence).ShouldEqual(2);
    [Fact] void should_contribute_exactly_the_three_inbox_sources() => _contributions.Keys.ShouldContainOnly([PreviousOwner, CurrentOwner, OverrideOwner]);
    [Fact] void should_contribute_only_the_primary_id_for_the_historical_source() => _contributions[PreviousOwner].ShouldContainOnly([new EventTypeId(EventId)]);
    [Fact] void should_deduplicate_the_primary_id_and_retain_the_join_id_for_the_current_source() => _contributions[CurrentOwner].ShouldContainOnly([new EventTypeId(EventId), new EventTypeId(JoinId)]);
    [Fact] void should_contribute_only_the_primary_id_for_the_explicit_inbox() => _contributions[OverrideOwner].ShouldContainOnly([new EventTypeId(EventId)]);
    [Fact] void should_not_duplicate_the_historical_contribution() => _contributions[PreviousOwner].Length.ShouldEqual(1);
    [Fact] void should_not_duplicate_the_current_contributions_across_definitions_or_clauses() => _contributions[CurrentOwner].Length.ShouldEqual(2);
    [Fact] void should_not_duplicate_the_override_contribution() => _contributions[OverrideOwner].Length.ShouldEqual(1);
    [Fact] void should_not_contribute_the_non_inbox_sequence() => _contributions.ContainsKey(CustomSequence).ShouldBeFalse();
    [Fact] void should_keep_the_previous_clause_generation_in_the_definitions() => _subject.Definitions.Single(definition => definition.EventSequenceId == "inbox-previous-owner").From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_current_generation_keys_in_the_current_inbox() => _subject.Definitions.Where(definition => definition.EventSequenceId == "inbox-current-owner").SelectMany(definition => definition.From.Keys).Any(eventType => eventType.Id == EventId && eventType.Generation == 2u).ShouldBeTrue();
}
