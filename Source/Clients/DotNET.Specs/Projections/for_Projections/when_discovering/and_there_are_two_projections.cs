// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_two_projections : given.all_dependencies
{
    public record FirstModel();
    public record SecondModel();

    IProjectionFor<FirstModel> _firstProjection;
    IProjectionFor<SecondModel> _secondProjection;

    Projections _projections;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _firstProjection = Substitute.For<IProjectionFor<FirstModel>>();
        _secondProjection = Substitute.For<IProjectionFor<SecondModel>>();

        _clientArtifacts.Projections.Returns(
        [
            _firstProjection.GetType(),
            _secondProjection.GetType()
        ]);

        _serviceProvider.GetService(_firstProjection.GetType()).Returns(_firstProjection);
        _serviceProvider.GetService(_secondProjection.GetType()).Returns(_secondProjection);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _projectionWatcherManager,
            _clientArtifacts,
            _namingPolicy,
            _eventSerializer,
            _serviceProvider,
            _jsonSerializerOptions);
    }

    async Task Because()
    {
        await _projections.Discover();
        _result = _projections.Definitions;
    }

    [Fact] void should_return_two_definitions() => _result.Count().ShouldEqual(2);
    [Fact] void should_call_define_on_first_model_builder() => _firstProjection.Received(1).Define(Arg.Any<IProjectionBuilderFor<FirstModel>>());
    [Fact] void should_call_define_on_second_model_builder() => _secondProjection.Received(1).Define(Arg.Any<IProjectionBuilderFor<SecondModel>>());
}
