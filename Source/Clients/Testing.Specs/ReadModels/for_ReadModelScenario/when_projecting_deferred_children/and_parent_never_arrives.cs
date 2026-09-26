// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_deferred_children;

public class and_parent_never_arrives : Specification
{
    ReadModelScenario<DeferredBoard> _scenario;
    EventSourceId _id;
    Guid _groupId;
    Exception _error;

    void Establish()
    {
        _scenario = new();
        _groupId = Guid.NewGuid();
        _id = new EventSourceId(_groupId);
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new DeferredItemAdded(_groupId, Guid.NewGuid(), "Item", 7));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_report_the_unresolved_parent() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_child_collection() => _error.Message.ShouldContain("groups");
    [Fact] void should_name_the_event_source() => _error.Message.ShouldContain(_id.Value);
    [Fact] void should_name_the_event_type() => _error.Message.ShouldContain(nameof(DeferredItemAdded));
}
