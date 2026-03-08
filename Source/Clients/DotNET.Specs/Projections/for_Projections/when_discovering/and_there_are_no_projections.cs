// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_no_projections : given.all_dependencies
{
    Projections _projections;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([]);
        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);
    }

    async Task Because()
    {
        await _projections.Discover();
        _result = _projections.Definitions;
    }

    [Fact] void should_return_empty_list() => _result.ShouldBeEmpty();
}
