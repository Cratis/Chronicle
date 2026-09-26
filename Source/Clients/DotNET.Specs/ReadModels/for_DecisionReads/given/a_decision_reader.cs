// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.given;

public class a_decision_reader : Specification
{
    protected IEventStore _store;
    protected IChronicleServicesAccessor _accessor;
    protected IServices _services;
    protected Contracts.ReadModels.IReadModels _readModels;
    protected Contracts.Sequences.IEventSequences _sequences;
    protected Contracts.Projections.IProjections _projectionService;
    protected Projections.Projections _projections;
    protected IReducers _reducers;
    protected IJsonSchemaGenerator _schemas;
    protected DecisionReads _reader;
    protected ProjectionDefinition _definition;
    protected int _folds;
    protected ulong _boundary = 5;
    protected ulong _probe = 4;
    protected ulong _last = 4;
    protected string _json = "{\"id\":\"source\"}";

    protected record Model(string Id);
    protected record NumericModel(int Id);
    protected record NoKeyModel(string Name);

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        _store.Name.Returns((EventStoreName)"store");
        _store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _accessor = (IChronicleServicesAccessor)connection;
        _store.Connection.Returns(connection);
        _services = Substitute.For<IServices>();
        _accessor.Services.Returns(_services);
        _readModels = Substitute.For<Contracts.ReadModels.IReadModels>();
        _sequences = Substitute.For<Contracts.Sequences.IEventSequences>();
        _projectionService = Substitute.For<Contracts.Projections.IProjections>();
        _services.ReadModels.Returns(_readModels);
        _services.Sequences.Returns(_sequences);
        _services.Projections.Returns(_projectionService);
        _reducers = Substitute.For<IReducers>();
        _store.Reducers.Returns(_reducers);
        _projections = new Projections.Projections(
            _store,
            Substitute.For<IEventTypes>(),
            Substitute.For<IClientArtifactsProvider>(),
            new DefaultNamingPolicy(),
            Substitute.For<IClientArtifactsActivator>(),
            new JsonSerializerOptions(),
            Substitute.For<ILogger<Projections.Projections>>());
        _definition = new ProjectionDefinition
        {
            Identifier = "projection", ReadModel = typeof(Model).GetReadModelIdentifier(), EventSequenceId = "event-log",
            From = new Dictionary<Contracts.Events.EventType, FromDefinition>
            {
                [new() { Id = "created", Generation = 1 }] = new() { Key = "$eventSourceId" }
            }
        };
        SetDefinitions(_definition);
        _schemas = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new DefaultNamingPolicy());
        _reader = new DecisionReads(_store, _projections, _schemas, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>()).Returns(_ =>
            new GetDefinitionsResponse
            {
                ReadModels = [new ReadModelDefinition
                {
                    Type = new() { Identifier = typeof(Model).GetReadModelIdentifier() },
                    ObserverIdentifier = "projection", ObserverType = ReadModelObserverType.Projection
                }]
            });
        _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
            .Returns(_ => new[] { _definition }.AsEnumerable());
        _sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>())
            .Returns(call => QueryResult<EventSequenceTailResponse>.Success(Guid.NewGuid(),
                new() { SequenceNumber = call.Arg<TailSequenceNumberRequest>().EventSourceId is null ? _boundary : _probe }));
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>())
            .Returns(_ =>
            {
                ++_folds;
                return new GetInstanceByKeyResponse { LastHandledEventSequenceNumber = _last, ReadModel = _json };
            });
        _readModels.DehydrateSession(Arg.Any<Contracts.ReadModels.DehydrateSessionRequest>(), Arg.Any<CallContext>()).Returns(Task.CompletedTask);
    }

    protected void SetDefinitions(params ProjectionDefinition[] definitions) =>
        typeof(Projections.Projections).GetProperty("Definitions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_projections, definitions.ToImmutableList());
}
