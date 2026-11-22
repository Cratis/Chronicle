// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_getting_the_registration_status;

public class and_the_registration_failed : Specification
{
    Contracts.Projections.RegistrationStatus _status;
    IRegistrationInbox _inbox;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        _inbox = Substitute.For<IRegistrationInbox>();
        _inbox.GetStatus(RegistrationKind.Projections, "studio").Returns(new Cratis.Chronicle.Registrations.RegistrationStatus(RegistrationIntentState.Failed, 9, 7, "the read model schema conflicts"));
        grainFactory.GetGrain<IRegistrationInbox>("event-store").Returns(_inbox);

        _service = new Projections(
            grainFactory,
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<ILanguageService>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<Chronicle.ReadModels.IReadModelsCompliance>(),
            Substitute.For<ILocalSiloDetails>(),
            AllowingConnectionOwnership.Create(),
            Microsoft.Extensions.Options.Options.Create(new Cratis.Chronicle.Configuration.ChronicleOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Projections>.Instance,
            NamespaceCasingContext.Create());
    }

    async Task Because() => _status = await _service.GetRegistrationStatus(new Contracts.Projections.GetRegistrationStatusRequest { EventStore = "event-store", Registrant = "studio" });

    [Fact] void should_report_failed() => _status.State.ShouldEqual(Contracts.Projections.RegistrationState.Failed);
    [Fact] void should_report_the_latest_revision() => _status.Revision.ShouldEqual(9);
    [Fact] void should_report_the_applied_revision() => _status.AppliedRevision.ShouldEqual(7);
    [Fact] void should_say_why() => _status.Failure.ShouldEqual("the read model schema conflicts");
}
