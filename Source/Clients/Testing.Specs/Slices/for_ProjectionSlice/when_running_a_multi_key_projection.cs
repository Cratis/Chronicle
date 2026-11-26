// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_running_a_multi_key_projection : Specification
{
    ProjectionSlice<JoinOrderSummary> _slice;
    Defaults _defaults;
    Exception _single;

    void Establish()
    {
        _defaults = new(new InMemoryClientArtifactsProvider { EventTypes = [typeof(JoinOrderPlaced)] });
        _slice = new(_defaults);
        _slice.Scenario.WithProjection(builder => builder.From<JoinOrderPlaced>(from => from.Set(model => model.Amount).To(@event => @event.Amount)));
        _slice.GivenEvents("b62fcdde-a6bd-41e4-a3d8-475d529678d2", new JoinOrderPlaced(new JoinCustomerId(Guid.Empty), 100m))
            .GivenEvents("5274d03c-bb63-44ec-9ee8-6b2064310680", new JoinOrderPlaced(new JoinCustomerId(Guid.Empty), 200m));
    }

    async Task Because()
    {
        var outcome = await _slice.Run();
        outcome.ShouldPartiallyMatchReadModel("b62fcdde-a6bd-41e4-a3d8-475d529678d2", new { Amount = 100m })
            .ShouldPartiallyMatchReadModel("5274d03c-bb63-44ec-9ee8-6b2064310680", new { Amount = 200m }).ShouldHaveNoReadModel("third");
        _single = Catch.Exception(() => _ = _slice.Scenario.Instance);
    }

    void Destroy()
    {
        _slice.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Fact] void should_materialize_all_keys_without_selecting_a_single_instance() => _slice.Scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_leave_the_existing_single_instance_guard_unchanged() => _single.ShouldBeOfExactType<MultipleInstancesMaterialized>();
}
