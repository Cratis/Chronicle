// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.given;

public class an_occurrence_client : Specification
{
    protected IEventStore _store;
    protected IChronicleConnection _connection;
    protected ConnectionLifecycle _lifecycle;
    protected Contracts.Occurrences.IOccurrences _service;
    protected IClientArtifactsProvider _artifacts;
    protected IJsonSchemaGenerator _schemas;
    protected IClientArtifactsActivator _activator;
    protected ServiceProvider _services;
    protected ICausationManager _causation;
    protected IIdentityProvider _identity;
    protected Occurrences _occurrences;
    private protected TriggerRegistry _registry;
    protected Subject<QueryResult<IEnumerable<OccurrenceSnapshotResponse>>> _notifications;
    protected TestOccurrenceHandler _handler;
    protected OccurrenceClaimDetails _claim;
    protected RegisterTriggersRequest _registration;
    protected RecordOccurrenceRequest _record;
    protected Exception _error;
    protected OccurrenceRecordResult _result;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(true);
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.Occurrences]);
        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        _connection.Lifecycle.Returns(_lifecycle);
        var contracts = Substitute.For<IServices>();
        _service = Substitute.For<Contracts.Occurrences.IOccurrences>();
        contracts.Occurrences.Returns(_service);
        ((IChronicleServicesAccessor)_connection).Services.Returns(contracts);
        _store = Substitute.For<IEventStore>();
        _store.Connection.Returns(_connection);
        _store.Name.Returns((EventStoreName)"store");
        _store.Namespace.Returns((EventStoreNamespaceName)"tenant");
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Triggers.Returns([typeof(TestTrigger)]);
        _schemas = Substitute.For<IJsonSchemaGenerator>();
        _schemas.Generate(typeof(TestTrigger)).Returns(JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"string\"}}}"));
        _services = new ServiceCollection().BuildServiceProvider();
        _activator = Substitute.For<IClientArtifactsActivator>();
        _handler = new();
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(TestOccurrenceHandler)).Returns(_ => new ActivatedArtifact(_handler, typeof(TestOccurrenceHandler), NullLogger<ActivatedArtifact>.Instance));
        _causation = Substitute.For<ICausationManager>();
        _causation.BeginScope(Arg.Any<CausationType>(), Arg.Any<IDictionary<string, string>>()).Returns(Substitute.For<IDisposable>());
        _identity = Substitute.For<IIdentityProvider>();
        _occurrences = new(_store);
        _occurrences.Configure(_artifacts, _schemas, new JsonSerializerOptions(JsonSerializerDefaults.Web), _services, _activator, _causation, _identity, NullLogger<Occurrences>.Instance);
        _registry = new(_store, _artifacts, _schemas);
        _notifications = new();
        _service.ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_notifications);
        _service.RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _registration = call.Arg<RegisterTriggersRequest>();
            return CommandResult<RegisterTriggersResponse>.Success(Guid.NewGuid(), new() { Registered = true });
        });
        _service.RecordOccurrence(Arg.Any<RecordOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _record = call.Arg<RecordOccurrenceRequest>();
            return CommandResult<RecordOccurrenceResponse>.Success(Guid.NewGuid(), new() { Id = "occurrence", Status = OccurrenceIngressStatus.Recorded });
        });
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new()));
        _service.CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new()));
        _service.FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new()));
        _service.RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new()));
        _claim = new() { Id = "occurrence", Trigger = "test-trigger", Payload = "{\"value\":\"hello\"}", Attempts = 2, Epoch = 3, RecordedAt = DateTimeOffset.UtcNow, LeaseExpiry = DateTimeOffset.UtcNow.AddMinutes(1) };
    }

    private protected OccurrenceAttempt CreateAttempt(TimeProvider? timeProvider = null) => new(
        _store,
        _service,
        typeof(TestTrigger),
        typeof(TestOccurrenceHandler),
        _services,
        _activator,
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        _causation,
        _identity,
        timeProvider ?? TimeProvider.System,
        NullLogger<Occurrences>.Instance);

    async Task Destroy()
    {
        await _occurrences.DisposeAsync();
        _notifications.Dispose();
        await _services.DisposeAsync();
    }
}
