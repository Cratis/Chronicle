// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Identities;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

using OccurrenceService = Cratis.Chronicle.Contracts.Occurrences.IOccurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Runs one attempt with isolated context, scoped activation and renewable ownership.
/// </summary>
/// <param name="eventStore">The namespace-bound store.</param>
/// <param name="service">The occurrence wire service.</param>
/// <param name="triggerType">The payload type.</param>
/// <param name="handlerType">The handler type.</param>
/// <param name="services">The root services for creating an attempt scope.</param>
/// <param name="activator">The artifact activator.</param>
/// <param name="jsonOptions">The payload serializer options.</param>
/// <param name="causation">The causation manager.</param>
/// <param name="identity">The identity provider.</param>
/// <param name="timeProvider">The renewal clock.</param>
/// <param name="logger">The worker logger.</param>
/// <param name="deliveryScope">Optional lifecycle delivery boundary.</param>
internal sealed class OccurrenceAttempt(
    IEventStore eventStore,
    OccurrenceService service,
    Type triggerType,
    Type handlerType,
    IServiceProvider services,
    IClientArtifactsActivator activator,
    JsonSerializerOptions jsonOptions,
    ICausationManager causation,
    IIdentityProvider identity,
    TimeProvider timeProvider,
    ILogger logger,
    OccurrenceDeliveryScope? deliveryScope = null)
{
    /// <summary>
    /// The requested lease duration, with renewal every twenty seconds.
    /// </summary>
    internal const int LeaseSeconds = 60;

    /// <summary>
    /// Invokes a handler and settles its lease, or abandons ownership on cancellation.
    /// </summary>
    /// <param name="claim">The durable claim.</param>
    /// <param name="cancellationToken">The connection/session cancellation.</param>
    /// <returns>The attempt work, including renewal shutdown.</returns>
    internal async Task Run(OccurrenceClaimDetails claim, CancellationToken cancellationToken)
    {
        var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var context = new OccurrenceContext(
            claim.Id,
            claim.Trigger,
            (OccurrenceKind)claim.Kind,
            claim.Due is null ? null : (DateTimeOffset)claim.Due,
            claim.RecordedAt,
            null,
            claim.Attempts,
            new(claim.Id, claim.Epoch))
        {
            Namespace = deliveryScope?.ApplicationScope == true ? null : new EventStoreNamespaceName(deliveryScope?.Namespace ?? eventStore.Namespace.Value)
        };
        CorrelationIdAccessor.SetCurrent(CorrelationId.New());
        identity.SetCurrentIdentity(Identity.System);
        var renewal = Renew(claim, lifetime);
        try
        {
            using var scope = causation.BeginScope("Occurrence", Properties(context));
            await Handle(claim, context, lifetime.Token);
        }
        finally
        {
            await lifetime.CancelAsync();
            await renewal;
            lifetime.Dispose();
            identity.ClearCurrentIdentity();
        }
    }

    static Dictionary<string, string> Properties(OccurrenceContext context)
    {
        var properties = new Dictionary<string, string>
        {
            ["occurrenceId"] = context.Id,
            ["triggerId"] = context.Trigger,
            ["kind"] = context.Kind.ToString(),
            ["recordedAt"] = context.RecordedAt.ToString("O", CultureInfo.InvariantCulture),
            ["attempt"] = context.Attempt.ToString(CultureInfo.InvariantCulture),
            ["epoch"] = context.Lease.Epoch.ToString(CultureInfo.InvariantCulture)
        };
        if (context.DueAt is { } due) properties["dueAt"] = due.ToString("O", CultureInfo.InvariantCulture);
        return properties;
    }

    async Task Handle(OccurrenceClaimDetails claim, OccurrenceContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Invoke(claim.Payload, context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disconnect, shutdown or supersession leaves the durable lease for reclamation.
            return;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var failure = new FailOccurrenceRequest
            {
                EventStore = eventStore.Name,
                Namespace = deliveryScope?.Namespace ?? eventStore.Namespace,
                ApplicationScope = deliveryScope?.ApplicationScope ?? false,
                Trigger = claim.Trigger,
                OccurrenceId = claim.Id,
                Epoch = claim.Epoch,
                Error = exception.ToString()
            };
            await service.FailOccurrence(failure, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
            return;
        }
        if (cancellationToken.IsCancellationRequested) return;
        var completion = new CompleteOccurrenceRequest
        {
            EventStore = eventStore.Name,
            Namespace = deliveryScope?.Namespace ?? eventStore.Namespace,
            ApplicationScope = deliveryScope?.ApplicationScope ?? false,
            Trigger = claim.Trigger,
            OccurrenceId = claim.Id,
            Epoch = claim.Epoch
        };
        var result = await service.CompleteOccurrence(completion, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
        if (result.Status is not (OccurrenceLeaseResult.Updated or OccurrenceLeaseResult.AlreadyCompleted or OccurrenceLeaseResult.OccurrenceLeaseLost))
        {
            throw new TriggerNotValid("The kernel returned an unknown completion result.");
        }
    }

    async Task Invoke(string payload, OccurrenceContext context, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var activation = activator.Activate(scope.ServiceProvider, handlerType);
        if (activation.TryGetException(out var failure)) throw failure;
        await using var artifact = activation.AsT0;
        var trigger = JsonSerializer.Deserialize(payload, triggerType, jsonOptions) ?? throw new TriggerNotValid("A trigger payload must not be null.");
        var method = typeof(IOccurrenceHandler<>).MakeGenericType(triggerType).GetMethod(nameof(IOccurrenceHandler<object>.Handle))!;
        try
        {
            await (Task)method.Invoke(artifact.Instance, [trigger, context, cancellationToken])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    async Task Renew(OccurrenceClaimDetails claim, CancellationTokenSource lifetime)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(LeaseSeconds / 3), timeProvider);
            while (await timer.WaitForNextTickAsync(lifetime.Token))
            {
                var renewal = new RenewOccurrenceLeaseRequest
                {
                    EventStore = eventStore.Name,
                    Namespace = deliveryScope?.Namespace ?? eventStore.Namespace,
                    ApplicationScope = deliveryScope?.ApplicationScope ?? false,
                    Trigger = claim.Trigger,
                    OccurrenceId = claim.Id,
                    Epoch = claim.Epoch,
                    LeaseSeconds = LeaseSeconds
                };
                var result = await service.RenewOccurrenceLease(renewal, new CallContext(new CallOptions(cancellationToken: lifetime.Token))).EnsureSuccess();
                if (result.Status != OccurrenceLeaseResult.Updated)
                {
                    await lifetime.CancelAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            // The attempt has ended; no renewal may survive it.
        }
        catch (Exception exception)
        {
            logger.OccurrenceLeaseRenewalFailed(exception, claim.Id);
            await lifetime.CancelAsync();
        }
    }
}
