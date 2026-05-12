// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_registration_fails : Specification
{
    Exception _exception;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Cratis.Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Cratis.Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        projectionsManager.Register(Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>())
            .Returns(Task.FromException(new InvalidOperationException("Failed to compile projection definition")));

        _service = new Services.Projections.Projections(
            grainFactory,
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<ILanguageService>(),
            Substitute.For<IServiceProvider>());
    }

    async Task Because() => _exception = await Catch.Exception(() => _service.Register(new RegisterRequest
    {
        EventStore = "event-store",
        Owner = ProjectionOwner.Server,
        Projections =
        [
            new ProjectionDefinition
            {
                EventSequenceId = "default",
                Identifier = "EmployeeListProjection",
                ReadModel = "EmployeeList",
                InitialModelState = "{}",
                All = new FromEveryDefinition()
            }
        ]
    }));

    [Fact] void should_throw_projection_registration_failure() => _exception.ShouldBeOfExactType<Services.Projections.ProjectionRegistrationFailed>();
    [Fact] void should_include_projection_identifier_in_message() => _exception.Message.ShouldContain("EmployeeListProjection");
    [Fact] void should_include_event_store_in_message() => _exception.Message.ShouldContain("event-store");
}
