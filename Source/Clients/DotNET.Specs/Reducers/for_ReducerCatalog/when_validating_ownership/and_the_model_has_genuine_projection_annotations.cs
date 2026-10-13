// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_validating_ownership;

public class and_the_model_has_genuine_projection_annotations : given.self_reducers
{
    Type _model;
    Exception _error;

    void Establish()
    {
        _model = EmitModel(new FromEventAttribute<OwnershipEvent>());
        _artifacts.Reducers.Returns([_model]);
        _artifacts.ModelBoundProjections.Returns([_model]);
        _store = CreateStore();
    }

    async Task Because() => _error = await Catch.Exception(_store.DiscoverAll);

    [Fact] void should_not_silently_choose_a_builder() => _error.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_not_register_the_invalid_model() => _store.Reducers.GetAllHandlers().ShouldBeEmpty();
}
