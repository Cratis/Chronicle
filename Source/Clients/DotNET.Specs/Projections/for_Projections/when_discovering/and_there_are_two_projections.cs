// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_there_are_two_projections : given.all_dependencies
{
    public record FirstModel();
    public record SecondModel();

    public class FirstProjection : IProjectionFor<FirstModel>
    {
        public bool DefineCalled { get; private set; }

        public void Define(IProjectionBuilderFor<FirstModel> builder) => DefineCalled = true;
    }

    public class SecondProjection : IProjectionFor<SecondModel>
    {
        public bool DefineCalled { get; private set; }

        public void Define(IProjectionBuilderFor<SecondModel> builder) => DefineCalled = true;
    }

    Projections _projections;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifacts.Projections.Returns(
        [
            typeof(FirstProjection),
            typeof(SecondProjection)
        ]);

        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<FirstModel>>(typeof(FirstProjection))
            .Returns(new FirstProjection());
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<SecondModel>>(typeof(SecondProjection))
            .Returns(new SecondProjection());

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

    [Fact] void should_return_two_definitions() => _result.Count().ShouldEqual(2);
}
