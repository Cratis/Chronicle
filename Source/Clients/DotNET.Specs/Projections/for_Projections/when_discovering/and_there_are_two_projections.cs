// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_two_projections : given.all_dependencies
{
    public record FirstModel();
    public record SecondModel();

    IProjectionFor<FirstModel> first_projection;
    IProjectionFor<SecondModel> second_projection;

    Projections projections;
    IEnumerable<ProjectionDefinition> result;

    void Establish()
    {
        first_projection = Substitute.For<IProjectionFor<FirstModel>>();
        second_projection = Substitute.For<IProjectionFor<SecondModel>>();

        _clientArtifacts.Projections.Returns(
        [
            first_projection.GetType(),
            second_projection.GetType()
        ]);

        _serviceProvider.GetService(first_projection.GetType()).Returns(first_projection);
        _serviceProvider.GetService(second_projection.GetType()).Returns(second_projection);

        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());

        projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _schemaGenerator,
            _modelNameResolver,
            _eventSerializer,
            _serviceProvider,
            _jsonSerializerOptions);
        projections.SetRulesProjections(_rulesProjections);

        _rulesProjections.Discover().Returns(ImmutableList<ProjectionDefinition>.Empty);
    }

    async Task Because()
    {
        await projections.Discover();
        result = projections.Definitions;
    }

    [Fact] void should_return_two_definitions() => result.Count().ShouldEqual(2);
    [Fact] void should_call_define_on_first_model_builder() => first_projection.Received(1).Define(Arg.Any<IProjectionBuilderFor<FirstModel>>());
    [Fact] void should_call_define_on_second_model_builder() => second_projection.Received(1).Define(Arg.Any<IProjectionBuilderFor<SecondModel>>());
}
