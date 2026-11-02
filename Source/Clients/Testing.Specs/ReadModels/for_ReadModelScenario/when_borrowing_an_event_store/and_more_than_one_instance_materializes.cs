// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_more_than_one_instance_materializes : a_borrowed_store
{
    ReadModelScenario<JoinOrderSummary> _borrowed;
    Exception _error;

    void Establish() => _borrowed = Borrowed<JoinOrderSummary>();

    async Task Because()
    {
        await _borrowed.Given.ForEventSource(new EventSourceId(Guid.NewGuid())).Events(new JoinOrderPlaced(new(Guid.NewGuid()), 100m));
        await _borrowed.Given.ForEventSource(new EventSourceId(Guid.NewGuid())).Events(new JoinOrderPlaced(new(Guid.NewGuid()), 200m));
        _error = Catch.Exception(() => _ = _borrowed.Instance);
    }

    [Fact] void should_reject_the_ambiguous_single_instance() => _error.ShouldBeOfExactType<MultipleInstancesMaterialized>();
    [Fact] void should_expose_both_instances() => _borrowed.Instances.Count.ShouldEqual(2);
}
