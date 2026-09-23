// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key;

/// <summary>
/// A fixed-key counting document whose root counts and whose children come and go, which is the shape a
/// live dashboard count takes.
/// </summary>
/// <remarks>
/// This specification exists because the harness once reported this projection working while the running
/// kernel dropped every child update and removal. Passing specifications were the only evidence anybody
/// had, and they were wrong. The scenario is deliberately identical to the integration scenario that runs
/// against a real kernel, so the two cannot report different answers without one of them going red.
/// </remarks>
public class and_the_root_also_counts : Specification
{
    ReadModelScenario<CountsReadModel> _scenario;
    EventSourceId _staysOpen;
    EventSourceId _getsClosed;
    EventSourceId _leavesTheSet;

    void Establish()
    {
        _scenario = new ReadModelScenario<CountsReadModel>();
        _staysOpen = new EventSourceId(Guid.NewGuid());
        _getsClosed = new EventSourceId(Guid.NewGuid());
        _leavesTheSet = new EventSourceId(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_staysOpen).Events(new CountedThingRegistered("stays open", true));
        await _scenario.Given.ForEventSource(_getsClosed).Events(new CountedThingRegistered("gets closed", true));
        await _scenario.Given.ForEventSource(_leavesTheSet).Events(new CountedThingRegistered("leaves the set", true));
        await _scenario.Given.ForEventSource(_getsClosed).Events(new CountedThingClosed());
        await _scenario.Given.ForEventSource(_leavesTheSet).Events(new CountedThingRemoved());
    }

    CountedThing? Thing(EventSourceId id) => _scenario.Instance.Things.SingleOrDefault(thing => thing.Id == id.Value);

    [Fact] void should_count_every_registration_at_the_root() => _scenario.Instance.Registrations.ShouldEqual(3);
    [Fact] void should_keep_the_things_that_did_not_leave() => _scenario.Instance.Things.Count().ShouldEqual(2);
    [Fact] void should_drop_the_thing_that_left() => Thing(_leavesTheSet).ShouldBeNull();
    [Fact] void should_carry_the_label_set_from_event_content() => Thing(_staysOpen).Label.ShouldEqual("stays open");
    [Fact] void should_apply_the_constant_set_by_the_closing_event() => Thing(_getsClosed).IsOpen.ShouldBeFalse();
    [Fact] void should_count_only_the_thing_still_open() => _scenario.Instance.Open.ShouldEqual(1);
}
