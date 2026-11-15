// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_the_definition_cannot_be_hashed : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;
    Exception _exception;

    void Establish()
    {
        _definition = CreateDefinition("the-projection", "the-read-model");
        _definition.InitialModelState["value"] = double.NaN;
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.Register([_definition]));

    [Fact] void should_complete_registration() => _exception.ShouldBeNull();
    [Fact] void should_store_the_definition() => _state.Projections.ShouldContainOnly(_definition);
    [Fact] void should_set_the_definition() => _projectionGrain.Received(1).SetDefinition(_definition);
}
