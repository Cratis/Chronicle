// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Specs.ReadModels.for_ReadModelInstance;

public class when_building_a_decision_scope
{
    readonly EventType _created = new("Created", EventTypeGeneration.First);
    readonly EventType _removed = new("Removed", EventTypeGeneration.First);

    [Fact]
    public void should_include_the_key_both_event_types_and_last_matching_number()
    {
        var read = new ReadModelInstance<string>("source", "present", 7, [_created, _removed]);
        var scope = read.ToConcurrencyScope();
        scope.EventSourceId.ShouldEqual((EventSourceId)"source");
        scope.SequenceNumber.ShouldEqual((EventSequenceNumber)7);
        scope.EventTypes.ShouldContain(_created);
        scope.EventTypes.ShouldContain(_removed);
    }

    [Fact]
    public void should_key_the_scope_by_the_read_source_for_an_append_to_another_source()
    {
        var read = new ReadModelInstance<string>("read-source", "present", 7, [_created, _removed]);
        var scopes = read.ToConcurrencyScopes();
        scopes.Count.ShouldEqual(1);
        scopes[(EventSourceId)"read-source"].EventSourceId.ShouldEqual((EventSourceId)"read-source");
        scopes[(EventSourceId)"read-source"].SequenceNumber.ShouldEqual((EventSequenceNumber)7);
    }

    [Fact]
    public void should_expect_no_matching_event_for_never_created()
    {
        var read = new ReadModelInstance<string>("source", null, EventSequenceNumber.Unavailable, [_created, _removed]);
        read.ToConcurrencyScope().ExpectsNoMatchingEvent.ShouldBeTrue();
    }

    [Fact]
    public void should_expect_the_removal_number_for_absence_after_removal()
    {
        var read = new ReadModelInstance<string>("source", null, 9, [_created, _removed]);
        read.ToConcurrencyScope().SequenceNumber.ShouldEqual((EventSequenceNumber)9);
    }

    [Fact]
    public void should_expect_no_matching_event_when_an_initial_state_produces_a_model_without_events()
    {
        var read = new ReadModelInstance<string>("source", "initial state", EventSequenceNumber.Unavailable, [_created]);
        read.ToConcurrencyScope().ExpectsNoMatchingEvent.ShouldBeTrue();
    }

    [Fact]
    public void should_refuse_a_missing_event_type_set()
    {
        var read = new ReadModelInstance<string>("source", "present", 7, []);
        Assert.Throws<InvalidOperationException>(read.ToConcurrencyScope);
    }
}
