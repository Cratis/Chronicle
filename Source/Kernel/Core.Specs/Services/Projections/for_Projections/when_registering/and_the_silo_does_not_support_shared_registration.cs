// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_silo_does_not_support_shared_registration : given.a_registration_with_a_connection
{
    Exception _exception;

    void Establish()
    {
        _request.FullSet = true;
        _request.Registrant = "the-app";
        _manager.RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), Arg.Any<Concepts.Projections.ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(Task.FromException(new TypeLoadException("The silo predates shared registration")));
    }

    async Task Because() => _exception = await Catch.Exception(() => _service.Register(_request));

    [Fact] void should_complete_registration() => _exception.ShouldBeNull();
    [Fact]
    void should_fall_back_preserving_the_registration_context() => _manager.Received(1).Register(
        Arg.Is<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(definitions => definitions.Single().Identifier == _projectionDefinition.Identifier),
        Concepts.Projections.ProjectionOwner.Client,
        "the-app",
        RegisteredVersion);
}
