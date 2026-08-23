// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.ExceptionExtensions;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_projection_registration_has_mixed_failures : given.a_startup_task
{
    Exception _infrastructureFailure;
    Exception _exception;

    void Establish()
    {
        var definition = CreateProjectionDefinition("projection", "read-model");
        var definitionFailure = new ProjectionDefinitionRegistrationFailed(
            definition.Identifier,
            new InvalidOperationException("Invalid definition"));
        _infrastructureFailure = new InvalidOperationException("Projection service unavailable");
        _projectionsManager.GetProjectionDefinitions().Returns([definition]);
        _projectionsServiceClient
            .Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>())
            .ThrowsAsync(new AggregateException(
                _infrastructureFailure,
                new ProjectionDefinitionsRegistrationFailed(
                    new Dictionary<ProjectionId, ProjectionDefinitionRegistrationFailed>
                    {
                        [definition.Identifier] = definitionFailure
                    })));
    }

    async Task Because() => _exception = await Catch.Exception(Execute);

    [Fact] void should_propagate_the_mixed_failure() => _exception.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_keep_the_infrastructure_failure() => ((AggregateException)_exception).InnerExceptions.ShouldContain(_infrastructureFailure);
    [Fact] void should_not_continue_rehydrating() => _jobsManager.DidNotReceive().Rehydrate();
}
