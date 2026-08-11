// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_child_collection_has_no_children;

/// <summary>
/// With the harness no longer inventing an answer, the empty collection a non-nullable declaration gets comes
/// from the reader resolving an absent field - the same code, on the same JSON, that a running system uses. The
/// two tiers agree because one produces the other's answer, not because two constants happen to match.
/// </summary>
/// <remarks>
/// This and its nullable sibling are the pair that makes the class catchable at all. Asserting only that an empty
/// child collection comes back empty measures nothing: it passed before the harness stopped pre-seeding and it
/// passes after. What has to be pinned is <em>where the answer comes from</em>, and the nullable case is the only
/// place a spec can see that.
/// <para>
/// The harness materializes twice - a single threaded result and one document per resolved key - and only the
/// first was ever asserted on, so the per-instance path could have been reading with a different set of
/// serializer options and no spec would have noticed. It is the path every multi-source spec asserts through.
/// </para>
/// </remarks>
public class and_it_is_declared_non_nullable : Specification
{
    ReadModelScenario<RosterWithRequiredMembers> _scenario;
    EventSourceId _rosterId;

    void Establish()
    {
        _scenario = new ReadModelScenario<RosterWithRequiredMembers>();
        _rosterId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_rosterId)
        .Events(new RosterOpened("Operations"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_honour_the_declared_type_rather_than_hand_back_null() => _scenario.Instance!.Members.ShouldNotBeNull();
    [Fact] void should_be_empty() => _scenario.Instance!.Members.ShouldBeEmpty();
    [Fact] void should_honour_the_declared_type_through_the_per_instance_view() => _scenario.InstanceForEventSourceId(_rosterId)!.Members.ShouldNotBeNull();
    [Fact] void should_be_empty_through_the_per_instance_view() => _scenario.InstanceForEventSourceId(_rosterId)!.Members.ShouldBeEmpty();
}
