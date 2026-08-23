// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.ExceptionExtensions;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_one_of_two_persisted_projection_definitions_is_rejected : given.a_startup_task
{
    ProjectionDefinition _acceptedDefinition;
    ProjectionDefinition _rejectedDefinition;
    Exception _exception;

    void Establish()
    {
        _acceptedDefinition = CreateProjectionDefinition("accepted-projection", "accepted-read-model");
        _rejectedDefinition = CreateProjectionDefinition("rejected-projection", "rejected-read-model");
        _projectionsManager.GetProjectionDefinitions().Returns([_acceptedDefinition, _rejectedDefinition]);
        _projectionsServiceClient
            .Register(
                _eventStore,
                Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Contains(_rejectedDefinition)))
            .ThrowsAsync(new ProjectionDefinitionRegistrationFailed(
                _rejectedDefinition.Identifier,
                new MissingChildCollectionInReadModelSchema(
                    _rejectedDefinition.Identifier,
                    "children",
                    _rejectedDefinition.ReadModel,
                    [])));
    }

    async Task Because() => _exception = await Catch.Exception(Execute);

    [Fact] void should_not_prevent_the_server_from_starting() => _exception.ShouldBeNull();
    [Fact] void should_register_the_accepted_definition_individually() => _projectionsServiceClient.Received(1).Register(_eventStore, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _acceptedDefinition })));
    [Fact] void should_attempt_to_register_the_rejected_definition_individually() => _projectionsServiceClient.Received(1).Register(_eventStore, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _rejectedDefinition })));
    [Fact] void should_continue_rehydrating_after_the_rejected_definition() => _jobsManager.Received(1).Rehydrate();
}
