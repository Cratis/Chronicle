// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

using ProjectionRegistrationError = Cratis.Chronicle.Projections.Engine.ProjectionRegistrationError;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Registering the persisted projection definitions is the likeliest call in the task to exceed the
/// response timeout, because it fans a subscribe out across every observer in the store instead of
/// doing one grain's work - so the budget shrinks as the store grows. It went unretried while every
/// other call was protected, and a production store grew into it: the timeout terminated the host,
/// the next attempt hit the same wall, and the kernel could not be started again at all.
/// </summary>
public class and_registering_persisted_projection_definitions_times_out : given.a_startup_task
{
    Exception _error;

    void Establish() =>
        _projectionsServiceClient.Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>())
            .Returns(
                _ => Task.FromException<Cratis.Monads.Result<ProjectionRegistrationError>>(new TimeoutException("Response did not arrive on time")),
                _ => Task.FromResult(Cratis.Monads.Result<ProjectionRegistrationError>.Success()));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_have_tried_registering_again() => await _projectionsServiceClient.Received(2).Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>());
}
