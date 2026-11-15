// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

public class a_double_projection : Specification
{
    protected ReadModelScenario<DoubleSeedModel> _scenario;

    void Establish() => _scenario = new ReadModelScenario<DoubleSeedModel>().WithProjection(builder => builder
        .From<DoubleSeedAdded>(from => from
            .UsingCompositeKey<DoubleSeedKey>(key => key.Set(part => part.Amount).To(@event => @event.Amount))
            .Increment(model => model.Count)));

    void Destroy() => _scenario.Dispose();
}
