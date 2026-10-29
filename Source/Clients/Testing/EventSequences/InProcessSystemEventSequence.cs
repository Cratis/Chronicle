// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Reflection;
using Cratis.Execution;
using KernelAuditing = KernelConcepts::Cratis.Chronicle.Concepts.Auditing;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelIdentities = KernelConcepts::Cratis.Chronicle.Concepts.Identities;
using KernelSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Stands in for the System event sequence in test scenarios: runs the two redaction requests synchronously
/// through the kernel's <c>EventSequencesReactor</c> and rejects every other operation.
/// </summary>
internal class InProcessSystemEventSequence : DispatchProxy
{
    KernelSequences::EventSequencesReactor _reactor = null!;
    KernelConceptsNs::EventStoreName _eventStore = null!;
    KernelConceptsNs::EventStoreNamespaceName _namespace = null!;

    /// <summary>
    /// Creates the System sequence stand-in.
    /// </summary>
    /// <param name="reactor">The kernel reactor handling the redaction requests.</param>
    /// <param name="eventStore">The event store the scenario runs in.</param>
    /// <param name="namespace">The namespace the scenario runs in.</param>
    /// <returns>The System sequence proxy.</returns>
    internal static KernelSequences::IEventSequence Create(
        KernelSequences::EventSequencesReactor reactor,
        KernelConceptsNs::EventStoreName eventStore,
        KernelConceptsNs::EventStoreNamespaceName @namespace)
    {
        var proxy = Create<KernelSequences::IEventSequence, InProcessSystemEventSequence>();
        var system = (InProcessSystemEventSequence)proxy;
        system._reactor = reactor;
        system._eventStore = eventStore;
        system._namespace = @namespace;
        return proxy;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        if (method.Name == nameof(KernelSequences::IEventSequence.Append) && args is { Length: 9 } && args[0] is KernelEvents::EventSourceId eventSourceId)
        {
            var context = ContextFor(eventSourceId, args[2] as CorrelationId, args[3] as IEnumerable<KernelAuditing::Causation>, args[4] as KernelIdentities::Identity);
            switch (args[1])
            {
                case KernelSequences::EventRedactionRequested request:
                    return Dispatch(() => _reactor.RedactionRequested(request, context), context);
                case KernelSequences::EventsRedactedForEventSource request:
                    return Dispatch(() => _reactor.RedactedForEventSource(request, context), context);
            }
        }

        var payload = args?.Length > 1 && args[1] is { } value ? $" for '{value.GetType().Name}'" : string.Empty;
        throw new GrainNotAvailableInTestScenario($"System event sequence operation '{method.Name}'{payload}");
    }

    static async Task<KernelSequences::AppendResult> Dispatch(Func<Task> handling, KernelEvents::EventContext context)
    {
        await handling();
        return KernelSequences::AppendResult.Success(context.CorrelationId, KernelEvents::EventSequenceNumber.Unavailable);
    }

    KernelEvents::EventContext ContextFor(
        KernelEvents::EventSourceId eventSourceId,
        CorrelationId? correlationId,
        IEnumerable<KernelAuditing::Causation>? causation,
        KernelIdentities::Identity? causedBy) =>
        KernelEvents::EventContext.From(
            _eventStore,
            _namespace,
            KernelEvents::EventType.Unknown,
            KernelEvents::EventSourceType.Default,
            eventSourceId,
            KernelEvents::EventStreamType.All,
            KernelEvents::EventStreamId.Default,
            KernelEvents::EventSequenceNumber.Unavailable,
            correlationId ?? CorrelationId.New()) with
        {
            Causation = causation ?? [],
            CausedBy = causedBy ?? KernelIdentities::Identity.System
        };
}
