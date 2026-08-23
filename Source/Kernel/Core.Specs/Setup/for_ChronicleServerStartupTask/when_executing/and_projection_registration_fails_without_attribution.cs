// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using NSubstitute.ExceptionExtensions;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_projection_registration_fails_without_attribution : given.a_startup_task
{
    Exception _registrationFailure;
    Exception _exception;

    void Establish()
    {
        var definition = CreateProjectionDefinition("projection", "read-model");
        _registrationFailure = new Exception("Projection service unavailable");
        _projectionsManager.GetProjectionDefinitions().Returns([definition]);
        _projectionsServiceClient
            .Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>())
            .ThrowsAsync(_registrationFailure);
    }

    async Task Because() => _exception = await Catch.Exception(Execute);

    [Fact] void should_propagate_the_failure() => _exception.ShouldEqual(_registrationFailure);
    [Fact] void should_not_continue_rehydrating() => _jobsManager.DidNotReceive().Rehydrate();
}
