// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_read_model_definition_is_empty : Specification
{
    ProjectionFactory _factory;
    ProjectionDefinition _definition;
    ReadModelDefinition _readModel;
    Exception _error;

    void Establish()
    {
        _factory = new(
            Substitute.For<IReadModelPropertyExpressionResolvers>(),
            Substitute.For<IEventValueProviderExpressionResolvers>(),
            Substitute.For<IKeyExpressionResolvers>(),
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<IKeyResolvers>(),
            Substitute.For<IStorage>(),
            NullLogger<ProjectionFactory>.Instance,
            Substitute.For<IEventCompliance>(),
            Substitute.For<IEventGenerationRelease>());
        _definition = Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "the-read-model");
        _readModel = (ReadModelDefinition)RuntimeHelpers.GetUninitializedObject(typeof(ReadModelDefinition));
    }

    async Task Because() => _error = await Catch.Exception(async () => await _factory.Create("store", "namespace", _definition, _readModel, []));

    [Fact] void should_fail_with_read_model_not_found() => _error.ShouldBeOfExactType<ReadModels.ReadModelNotFound>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain("the-read-model");
}
