// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_kernel_accepts_the_registration : Specification
{
    Exception _exception;
    Chronicle.Projections.IProjectionsManager _projectionsManager;
    IRegistrationInbox _inbox;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        _projectionsManager = projectionsManager;
        _inbox = Substitute.For<IRegistrationInbox>();
        _inbox.AcceptProjections(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), Arg.Any<Concepts.Projections.ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(new RegistrationAcceptance(4, RegistrationIntentState.Pending));
        grainFactory.GetGrain<IRegistrationInbox>(Arg.Any<string>()).Returns(_inbox);

        _service = new Projections(
            grainFactory,
            Substitute.For<IExpandoObjectConverter>(),
            Cratis.Chronicle.Events.for_PublicEventContentRedactor.given.header_events.Options,
            Substitute.For<ILanguageService>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<Chronicle.ReadModels.IReadModelsCompliance>(),
            Substitute.For<ILocalSiloDetails>(),
            AllowingConnectionOwnership.Create(),
            Microsoft.Extensions.Options.Options.Create(new Cratis.Chronicle.Configuration.ChronicleOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Projections>.Instance,
            NamespaceCasingContext.Create());
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

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_hand_the_registration_to_the_inbox() => _inbox.ReceivedWithAnyArgs(1).AcceptProjections(default!, default!, default, default, default);
    [Fact] void should_not_apply_it_before_answering() => _projectionsManager.ReceivedCalls().ShouldBeEmpty();
}
