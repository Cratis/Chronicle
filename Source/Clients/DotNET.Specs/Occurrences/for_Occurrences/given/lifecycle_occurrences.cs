// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Occurrences.given;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.given;

public class lifecycle_occurrences : an_occurrence_client
{
    protected readonly ChronicleClientOptions _options = new() { EventStore = "store", ActivationId = "rollout" };
    protected readonly List<RecordLifecycleOccurrenceRequest> _recordings = [];
    protected readonly List<ObserveOccurrencesRequest> _observations = [];

    void Establish()
    {
        _artifacts.OccurrenceHandlers.Returns([typeof(OnStartup), typeof(OnShutdown)]);
        _schemas.Generate(typeof(Startup)).Returns(JsonSchema.FromJson("{\"type\":\"object\"}"));
        _schemas.Generate(typeof(Shutdown)).Returns(JsonSchema.FromJson("{\"type\":\"object\"}"));
        _occurrences.Dispose();
        _occurrences = new(_store);
        _occurrences.Configure(_artifacts, _schemas, new JsonSerializerOptions(), _services, _activator, _causation, _identity, NullLogger<Occurrences>.Instance, _options);
        _store.Occurrences.Returns(_occurrences);
        _service.RecordLifecycleOccurrence(Arg.Any<RecordLifecycleOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            var request = call.Arg<RecordLifecycleOccurrenceRequest>();
            _recordings.Add(request);
            return CommandResult<RecordLifecycleOccurrenceResponse>.Success(Guid.NewGuid(), new()
            {
                Id = LifecycleIdentity.Calculate(request.Kind.ToString().ToLowerInvariant(), new(request.ActivationId), null),
                Status = OccurrenceIngressStatus.Recorded,
                Namespaces = request.AllNamespaces ? ["first", "second"] : []
            });
        });
        _service.ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            var request = call.Arg<ObserveOccurrencesRequest>();
            _observations.Add(request);
            var activation = LifecycleActivation.Resolve(_options);
            return Observable.Return(QueryResult<IEnumerable<OccurrenceSnapshotResponse>>.Success(Guid.NewGuid(), [new()
            {
                Id = LifecycleIdentity.Calculate("shutdown", activation, request.Namespace),
                State = Contracts.Storage.Occurrences.OccurrenceState.Completed
            }]));
        });
    }

    sealed class OnStartup : IOccurrenceHandler<Startup>
    {
        Task IOccurrenceHandler<Startup>.Handle(Startup trigger, OccurrenceContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    sealed class OnShutdown : IOccurrenceHandler<Shutdown>
    {
        Task IOccurrenceHandler<Shutdown>.Handle(Shutdown trigger, OccurrenceContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
