// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_child_collection_has_no_children;

/// <summary>
/// The harness must not answer this question on its own. A children-collection path is written only by
/// <c>ChildAdded</c> / <c>ChildRemoved</c> - the live pipeline removes every such path from the initial-state
/// diff, deliberately, because writing <c>[]</c> from a root event would race a sibling partition's already-added
/// child away - so a collection with no children is an absent field in the store.
/// <para>
/// The harness used to seed <c>[]</c> for every array-typed schema property and stop there, reproducing the
/// kernel's seeding step and omitting the kernel's very next step. That put the representation of an empty child
/// collection out of reach of every spec at every tier: a consumer could have complete, green coverage of exactly
/// the path that dereferences null in production. A nullable declaration is what makes the difference visible -
/// with the pre-seed it is <c>[]</c>, without it the field is absent and stays absent.
/// </para>
/// </summary>
public class and_it_is_declared_nullable : Specification
{
    ReadModelScenario<RosterWithOptionalMembers> _scenario;
    EventSourceId _rosterId;

    void Establish()
    {
        _scenario = new ReadModelScenario<RosterWithOptionalMembers>();
        _rosterId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_rosterId)
        .Events(new RosterOpened("Operations"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_project_the_root() => _scenario.Instance!.GroupName.ShouldEqual("Operations");
    [Fact] void should_leave_the_child_collection_absent() => _scenario.Instance!.Members.ShouldBeNull();
}
