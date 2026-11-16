// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Carries the outcome in the existing delivery state machine without a per-partition heap context.
    /// </summary>
    /// <param name="tail">The initial observation position.</param>
    struct HandlingOutcome(EventSequenceNumber tail)
    {
        /// <summary>Gets whether delivery failed.</summary>
        public bool Failed { get; private set; }

        /// <summary>Gets the failure classification.</summary>
        public FailureKind FailureKind { get; private set; } = FailureKind.Unknown;

        /// <summary>Gets the exception raised while handling, if any.</summary>
        public Exception? FailureException { get; private set; }

        /// <summary>Gets the subscriber's failure policy.</summary>
        public WebhookDeliveryFailurePolicy FailurePolicy { get; private set; } = WebhookDeliveryFailurePolicy.Retry;

        /// <summary>Gets the failure messages.</summary>
        public IEnumerable<string> ExceptionMessages { get; private set; } = Enumerable.Empty<string>();

        /// <summary>Gets the failure stack trace.</summary>
        public string ExceptionStackTrace { get; private set; } = string.Empty;

        /// <summary>Gets or sets the observation position to record on failure.</summary>
        public EventSequenceNumber Tail { get; set; } = tail;

        /// <summary>Gets or sets the delivery fence.</summary>
        public DeliveryFenceContext? DeliveryFence { get; set; }

        /// <summary>Gets or sets whether handling changed observer state.</summary>
        public bool StateChanged { get; set; }

        /// <summary>Gets or sets the counts to coalesce after handling.</summary>
        public IReadOnlyDictionary<EventTypeId, EventCount> HandledCounts { get; set; } = ImmutableDictionary<EventTypeId, EventCount>.Empty;

        /// <summary>Records a failed subscriber result.</summary>
        /// <param name="result">The subscriber result.</param>
        /// <param name="firstSequenceNumber">The first event delivered.</param>
        public void RecordResult(ObserverSubscriberResult result, EventSequenceNumber firstSequenceNumber)
        {
            if (result.State != ObserverSubscriberState.Failed) return;
            Failed = true;
            FailureKind = FailureKind.Handling;
            FailurePolicy = result.FailurePolicy;
            ExceptionMessages = result.ExceptionMessages;
            ExceptionStackTrace = result.ExceptionStackTrace;
            Tail = result.HandledAnyEvents ? result.LastSuccessfulObservation : firstSequenceNumber;
        }

        /// <summary>Records an exception without replacing a policy already supplied by the subscriber.</summary>
        /// <param name="exception">The handling exception.</param>
        public void RecordException(Exception exception)
        {
            Failed = true;
            FailureKind = exception.ToFailureKind();
            FailureException = exception;
            ExceptionMessages = exception.GetAllMessages().ToArray();
            ExceptionStackTrace = exception.StackTrace ?? string.Empty;
        }
    }
}
