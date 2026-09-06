// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_multiple_definitions_in_the_batch_fail : Specification
{
    Exception _exception;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        projectionsManager.Register(Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>())
            .Returns(Task.FromException(new SomeProjectionDefinitionsFailedToRegister(
                (EventStoreName)"event-store",
                new Dictionary<Concepts.Projections.ProjectionId, Exception>
                {
                    [(Concepts.Projections.ProjectionId)"FirstProjection"] = new ProjectionDefinitionRegistrationFailed("FirstProjection", new InvalidOperationException("first failure")),
                    [(Concepts.Projections.ProjectionId)"SecondProjection"] = new ProjectionDefinitionRegistrationFailed("SecondProjection", new InvalidOperationException("second failure"))
                })));

        _service = new Projections(
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
                Identifier = "FirstProjection",
                ReadModel = "First",
                InitialModelState = "{}",
                All = new FromEveryDefinition()
            },
            new ProjectionDefinition
            {
                EventSequenceId = "default",
                Identifier = "SecondProjection",
                ReadModel = "Second",
                InitialModelState = "{}",
                All = new FromEveryDefinition()
            }
        ]
    }));

    [Fact] void should_throw_projection_registration_failure() => _exception.ShouldBeOfExactType<ProjectionRegistrationFailed>();
    [Fact] void should_name_the_first_definition_that_failed() => _exception.Message.ShouldContain("FirstProjection");
    [Fact] void should_name_the_second_definition_that_failed() => _exception.Message.ShouldContain("SecondProjection");
    [Fact] void should_include_event_store_in_message() => _exception.Message.ShouldContain("event-store");
}
