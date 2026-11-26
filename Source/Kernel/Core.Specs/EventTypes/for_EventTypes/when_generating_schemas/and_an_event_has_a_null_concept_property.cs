// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_generating_schemas;

public class and_an_event_has_a_null_concept_property : Specification
{
    EventTypes _eventTypes;
    EventSerializer _serializer;
    IKernelEventProof _proof;
    JsonSchema _schema;
    JsonObject _unproven;
    JsonObject _proven;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(AdministratorPrivilegeMigrated)]);
        var storage = Substitute.For<IStorage>();
        var eventTypesStorage = Substitute.For<IEventTypesStorage>();
        storage.GetEventStore(EventStoreName.System).EventTypes.Returns(eventTypesStorage);
        eventTypesStorage.Register(Arg.Any<EventType>(), Arg.Any<JsonSchema>(), EventTypeOwner.Server, EventTypeSource.Code)
            .Returns(call =>
            {
                _schema = call.Arg<JsonSchema>();
                return Task.FromResult(true);
            });
        _eventTypes = new(types, storage, NullLogger<EventTypes>.Instance);
        var services = new ServiceCollection();
        var siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);
        siloBuilder.ConfigureSerialization();
        var options = (JsonSerializerOptions)services.Single(service => service.ServiceType == typeof(JsonSerializerOptions)).ImplementationInstance!;
        _serializer = new(_eventTypes, Substitute.For<IExpandoObjectConverter>(), options);
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
    }

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(EventStoreName.System);
        _unproven = Serialize(new("Unproven", UserId.NotSet));
        _proven = Serialize(new("Proven", Guid.NewGuid()));
    }

    [Fact] void should_accept_the_unproven_migration() => _schema.Validate(_unproven).ShouldBeEmpty();
    [Fact] void should_accept_the_proven_migration() => _schema.Validate(_proven).ShouldBeEmpty();
    [Fact] void should_keep_the_previously_generated_schema() => _schema.ToJson().ShouldEqual(_eventTypes.GetJsonSchema(typeof(PreviousMigrationShape)).ToJson());
    [Fact] void should_serialize_the_unproven_user_sentinel() => _unproven["userId"]!.GetValue<Guid>().ShouldEqual(UserId.NotSet.Value);

    JsonObject Serialize(AdministratorPrivilegeMigrated migration) => _serializer.Serialize(
        RosterHistory.WithProof(migration, Roster.MigrationEventSourceId, EventSequenceNumber.BeforeFirst, _proof));

    /// <summary>
    /// Preserves the original nullable CLR shape without registering another event type.
    /// </summary>
    /// <param name="Outcome">The migration outcome.</param>
    /// <param name="UserId">The proven user, when any.</param>
    /// <param name="Proof">The kernel proof.</param>
    record PreviousMigrationShape(string Outcome, UserId? UserId, string Proof = "");
}
