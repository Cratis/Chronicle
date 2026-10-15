// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

/// <summary>
/// Applications sharing an event store each claim a full set. A client that does not identify itself cannot be
/// attributed, so its full set must not retire anything - otherwise it would retire every other application's projections.
/// </summary>
public class and_the_full_set_has_no_registrant : Specification
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
            Substitute.For<IServiceProvider>(),
            Substitute.For<Chronicle.ReadModels.IReadModelsCompliance>());
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
    void should_not_retire_anything() =>
        _projectionsManager.Received(1).Register(
            Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(),
            null,
            null);
}
