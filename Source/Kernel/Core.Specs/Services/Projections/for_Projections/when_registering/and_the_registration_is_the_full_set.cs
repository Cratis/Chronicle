// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_registration_is_the_full_set : Specification
{
    Chronicle.Projections.IProjectionsManager _projectionsManager;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        _projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(_projectionsManager);

        _service = new Projections(
            grainFactory,
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<ILanguageService>(),
            Substitute.For<IServiceProvider>());
    }

    async Task Because() => await _service.Register(new RegisterRequest
    {
        EventStore = "event-store",
        Owner = ProjectionOwner.Client,
        FullSet = true,
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
    });

    [Fact]
    void should_register_as_the_full_set_for_the_owner() =>
        _projectionsManager.Received(1).Register(
            Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(),
            Concepts.Projections.ProjectionOwner.Client);
}
