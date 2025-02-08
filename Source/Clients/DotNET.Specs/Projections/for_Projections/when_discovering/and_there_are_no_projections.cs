// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_no_projections : given.all_dependencies
{
    Projections projections;
    IEnumerable<ProjectionDefinition> result;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([]);
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

    [Fact] void should_return_empty_list() => result.ShouldBeEmpty();
}
