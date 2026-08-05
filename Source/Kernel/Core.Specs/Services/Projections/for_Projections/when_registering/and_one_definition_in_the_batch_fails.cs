// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_one_definition_in_the_batch_fails : Specification
{
    Exception _exception;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        projectionsManager.Register(Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>())
            .Returns(Task.FromException(new ProjectionDefinitionRegistrationFailed(
                "SecondProjection",
                new InvalidOperationException("Failed to compile projection definition"))));

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
    [Fact] void should_name_the_definition_that_failed() => _exception.Message.ShouldContain("SecondProjection");
    [Fact] void should_not_name_the_definitions_that_did_not() => _exception.Message.Contains("FirstProjection", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_include_event_store_in_message() => _exception.Message.ShouldContain("event-store");
    [Fact] void should_keep_the_root_cause() => _exception.GetBaseException().Message.ShouldEqual("Failed to compile projection definition");
}
