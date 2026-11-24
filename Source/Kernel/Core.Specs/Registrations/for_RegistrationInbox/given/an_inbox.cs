// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox.given;

public class an_inbox : Specification
{
    protected const string EventStore = "the-event-store";

    protected TestKitSilo _silo;
    protected RegistrationInbox _grain;
    protected FakeRegistrationIntentStorage _intents;
    protected FakeRegistrationPayloads _payloads;
    protected ManualTimeProvider _clock;
    protected IProjectionsManager _manager;
    protected IEventStoreStorage _eventStoreStorage;
    protected IRegistrationBacklog _backlog;

    protected virtual ChronicleOptions Options => new() { Registration = new() { MaxApplyAttempts = 2, ApplyRetryDelaySeconds = 5 } };

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _intents = new();
        _payloads = new();
        _clock = new();
        _manager = Substitute.For<IProjectionsManager>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventStoreStorage.RegistrationIntents.Returns(_intents);
        _backlog = Substitute.For<IRegistrationBacklog>();
        _silo.AddService(_backlog);

        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _silo.AddService(storage);
        _silo.AddService<IRegistrationPayloads>(_payloads);
        _silo.AddService<IOptions<ChronicleOptions>>(Microsoft.Extensions.Options.Options.Create(Options));
        _silo.AddService<TimeProvider>(_clock);
        _silo.AddProbe(_ => _manager);
        PrepareBeforeActivation();
        _grain = await _silo.CreateGrainAsync<RegistrationInbox>(EventStore);
    }

    protected virtual void PrepareBeforeActivation()
    {
    }

    protected static ProjectionDefinition CreateDefinition(string identifier) => Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition(identifier, "read-model");

    protected static async Task WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 300 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }
    }

    protected Task<RegistrationAcceptance> Accept(ProjectionDefinition[] definitions, string registrant, ProjectionOwner? fullSetOwner = ProjectionOwner.Client, string version = "1.0") =>
        _grain.AcceptProjections(ProjectionRegistrationSignature.For(definitions, fullSetOwner, registrant, version), definitions, fullSetOwner, registrant, version);
}
