// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// The failure shape behind https://github.com/Cratis/Chronicle/issues/3722: the engine rejects one definition in
/// the changed batch. The rejection must cost that definition alone - every other changed definition still lands
/// in the engine, in its projection grain and in the registered state, so it is not silently stale afterwards.
/// </summary>
public class and_the_engine_rejects_one_of_two_changed_definitions : given.a_projections_manager_grain
{
    ProjectionDefinition _accepted;
    ProjectionDefinition _rejected;
    Exception _exception;

    void Establish()
    {
        _accepted = CreateDefinition("accepted-projection", "accepted-read-model");
        _rejected = CreateDefinition("rejected-projection", "rejected-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("accepted-read-model"), CreateReadModelDefinition("rejected-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.New);

        _projectionsServiceClient
            .Register(
                (EventStoreName)EventStore,
                Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Contains(_rejected)))
            .ThrowsAsync(new ProjectionDefinitionRegistrationFailed(_rejected.Identifier, new InvalidOperationException("Failed to compile projection definition")));
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.Register([_accepted, _rejected]));

    [Fact] void should_throw_naming_the_rejected_definition() => _exception.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_report_the_failure_for_the_rejected_definition_only() => ((SomeProjectionDefinitionsFailedToRegister)_exception).Failures.Keys.ShouldContainOnly((ProjectionId)"rejected-projection");
    [Fact] void should_register_the_accepted_definition_with_the_engine_on_its_own() => _projectionsServiceClient.Received(1).Register((EventStoreName)EventStore, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _accepted })));
    [Fact] void should_set_the_definition_on_the_accepted_projection_grain() => _projectionGrain.Received(1).SetDefinition(_accepted);
    [Fact] void should_not_set_the_definition_on_the_rejected_projection_grain() => _projectionGrain.DidNotReceive().SetDefinition(_rejected);
    [Fact] void should_keep_only_the_accepted_definition_in_the_registered_state() => _state.Projections.ShouldContainOnly(_accepted);
}
