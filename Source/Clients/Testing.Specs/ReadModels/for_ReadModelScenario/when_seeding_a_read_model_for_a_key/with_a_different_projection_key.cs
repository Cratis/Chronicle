// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_a_different_projection_key : Specification
{
    ReadModelScenario<SeededKeyModel> _scenario;

    void Establish()
    {
        _scenario = new ReadModelScenario<SeededKeyModel>().WithProjection(builder =>
            builder.From<SeededKeyRenamed>(from => from.UsingKey(@event => @event.Caption).Count(model => model.Count)));
        _scenario.Given.ForEventSourceId("invoice-1").ReadModel(new("invoice-1", "seeded", "retained", 5));
    }

    async Task Because() => await _scenario.Given.ForEventSourceId("different-source").Events(new SeededKeyRenamed("invoice-1"));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_apply_the_seed_to_the_resolved_read_model_key() => _scenario.InstanceForEventSourceId("invoice-1")!.Count.ShouldEqual(6);
    [Fact] void should_not_materialize_the_input_event_source() => _scenario.InstanceForEventSourceId("different-source").ShouldBeNull();
}
