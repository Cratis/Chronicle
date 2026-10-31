// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences;

public partial class Occurrences
{
    /// <summary>
    /// Gets whether the store participates in host lifecycle recording.
    /// </summary>
    internal bool HasLifecycleHandlers => _triggers?.HasLifecycleHandlers == true;

    /// <inheritdoc/>
    public async Task EndActivation(ActivationId activationId) => await RecordLifecycle(activationId, false, CancellationToken.None);

    /// <summary>
    /// Records the host activation after registration succeeds.
    /// </summary>
    /// <param name="cancellationToken">The host startup deadline.</param>
    /// <returns>The durable startup recording.</returns>
    internal async Task StartActivation(CancellationToken cancellationToken)
    {
        if (!HasLifecycleHandlers) return;
        await RecordLifecycle(LifecycleActivation.Resolve(_options), true, cancellationToken);
    }

    /// <summary>
    /// Records and observes per-process shutdown within the host deadline.
    /// </summary>
    /// <param name="cancellationToken">The host shutdown deadline.</param>
    /// <returns>The best-effort shutdown work.</returns>
    internal async Task StopActivation(CancellationToken cancellationToken)
    {
        if (!HasLifecycleHandlers || _options.ActivationScope != ActivationScope.PerProcess) return;
        var activationId = LifecycleActivation.Resolve(_options);
        var result = await RecordLifecycle(activationId, false, cancellationToken);
        if (!_triggers!.Handlers.Any(_ => _.Trigger == typeof(Shutdown))) return;
        var scopes = result.Namespaces.Select(name => new OccurrenceDeliveryScope(false, name));
        if (_options.LifecycleScope == LifecycleScope.Application) scopes = [new(true, null)];
        await Task.WhenAll(scopes.Select(scope => WaitForShutdown(scope, activationId, cancellationToken)));
    }

    async Task<RecordLifecycleOccurrenceResponse> RecordLifecycle(ActivationId activationId, bool startup, CancellationToken cancellationToken)
    {
        OccurrenceSupport.Require(eventStore.Connection);
        if (string.IsNullOrWhiteSpace(activationId.Value)) throw new ActivationIdRequired();
        var request = new RecordLifecycleOccurrenceRequest
        {
            EventStore = eventStore.Name,
            ActivationId = activationId.Value,
            Kind = startup ? Contracts.Storage.Occurrences.OccurrenceKind.Startup : Contracts.Storage.Occurrences.OccurrenceKind.Shutdown,
            AllNamespaces = _options.LifecycleScope == LifecycleScope.AllNamespaces
        };
        var services = await ((IChronicleServicesAccessor)eventStore.Connection).GetServices(cancellationToken);
        var result = await services.Occurrences.RecordLifecycleOccurrence(request, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
        if (result.Status is not (OccurrenceIngressStatus.Recorded or OccurrenceIngressStatus.AlreadyRecorded))
        {
            throw new TriggerNotValid($"Lifecycle recording was refused: {result.Status}.");
        }
        AddLifecycleNamespaces(result.Namespaces);
        return result;
    }

    async Task WaitForShutdown(OccurrenceDeliveryScope scope, ActivationId activationId, CancellationToken cancellationToken)
    {
        var id = LifecycleIdentity.Calculate("shutdown", activationId, scope.Namespace);
        var request = new ObserveOccurrencesRequest
        {
            EventStore = eventStore.Name,
            Namespace = scope.Namespace,
            ApplicationScope = scope.ApplicationScope,
            Trigger = "shutdown"
        };
        var services = await ((IChronicleServicesAccessor)eventStore.Connection).GetServices(cancellationToken);
        await services.Occurrences.ObserveOccurrences(request, new CallContext(new CallOptions(cancellationToken: cancellationToken)))
            .Select(result => result.EnsureSuccess())
            .FirstAsync(items => items.Any(item => item.Id == id && item.State == OccurrenceState.Completed))
            .ToTask(cancellationToken);
    }
}
