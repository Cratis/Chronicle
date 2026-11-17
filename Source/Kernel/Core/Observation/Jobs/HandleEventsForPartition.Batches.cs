// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForPartition
{
    /// <summary>
    /// The sequence numbers a performing step keeps track of, shared with the handlers that end it.
    /// </summary>
    sealed class PerformProgress
    {
        public EventSequenceNumber LastHandled { get; set; } = EventSequenceNumber.Unavailable;

        public EventSequenceNumber LastAttempted { get; set; } = EventSequenceNumber.Unavailable;

        public EventSequenceNumber LastScanned { get; set; } = EventSequenceNumber.Unavailable;
    }

    /// <summary>
    /// What a step needs to deliver the batches of one run to its subscriber.
    /// </summary>
    /// <param name="State">The state the step was performed with.</param>
    /// <param name="Events">The cursor over the events to deliver.</param>
    /// <param name="Context">The context handed to the subscriber.</param>
    /// <param name="Timeout">How long the subscriber has to answer a call.</param>
    /// <param name="NonRedactionEventTypeIds">The ids of the event types read that are not redactions.</param>
    /// <param name="Filters">The observer's filters, if any.</param>
    sealed record DeliveryPlan(
        HandleEventsForPartitionState State,
        BoundedEventCursor Events,
        ObserverSubscriberContext Context,
        TimeSpan Timeout,
        HashSet<EventTypeId> NonRedactionEventTypeIds,
        ObserverFilters? Filters);

    /// <summary>
    /// What the subscriber answered for one batch, and the events it was given.
    /// </summary>
    /// <param name="Result">The subscriber's answer.</param>
    /// <param name="HandledEvents">The events the subscriber was given.</param>
    sealed record ObservedBatch(ObserverSubscriberResult Result, AppendedEvent[] HandledEvents);

    /// <summary>
    /// Why a batch failed.
    /// </summary>
    /// <param name="Messages">The failure messages.</param>
    /// <param name="StackTrace">The stack trace.</param>
    /// <param name="Kind">The kind of failure.</param>
    /// <param name="Policy">The delivery recovery policy.</param>
    sealed record BatchFailure(
        string[] Messages,
        string StackTrace,
        FailureKind Kind,
        WebhookDeliveryFailurePolicy Policy = WebhookDeliveryFailurePolicy.Retry);

    /// <summary>
    /// What came of applying one batch: carry on, stop with a result, or fail the batch.
    /// </summary>
    /// <param name="Result">The result that ends the step, if the batch ends it.</param>
    /// <param name="Failure">The failure that is to be reported, if the batch failed.</param>
    sealed record BatchOutcome(JobStepResult? Result = null, BatchFailure? Failure = null)
    {
        public static BatchOutcome Continue { get; } = new();

        public static BatchOutcome Stop(JobStepResult result) => new(Result: result);

        public static BatchOutcome Fail(BatchFailure failure) => new(Failure: failure);
    }
}
