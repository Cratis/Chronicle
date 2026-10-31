// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A registration that failed is never remembered as done, so the identical one that follows - the client retrying - runs.
/// </summary>
public class and_the_first_registration_fails_before_an_identical_one : given.a_projections_manager_grain
{
    const int DefinitionCount = 20;

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _state.Projections = [.. Definitions()];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because()
    {
        _projectionsServiceClient
            .Register(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionDefinition>>())
            .Returns(
                Task.FromException<Cratis.Monads.Result<ProjectionRegistrationError>>(new InvalidOperationException("the engine is unavailable")),
                Task.FromResult(Cratis.Monads.Result<ProjectionRegistrationError>.Success()));

        _state.Projections = [];
        _failure = await Catch.Exception(() => _grain.Register(Definitions(), registrant: "the-app", version: "1.0"));
        await _grain.Register(Definitions(), registrant: "the-app", version: "1.0");
    }

    [Fact] void should_fail_the_first_registration() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_register_with_the_engine_again() => _projectionsServiceClient.ReceivedWithAnyArgs(2).Register(default!, default!);

    static ProjectionDefinition[] Definitions() => [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];

    Exception _failure;
}
