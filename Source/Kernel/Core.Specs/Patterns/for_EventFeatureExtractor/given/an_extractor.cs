// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.given;

public class an_extractor : Specification
{
    protected static readonly DateTimeOffset Occurred = new(2026, 8, 24, 9, 15, 0, TimeSpan.Zero);
    protected static readonly CorrelationId Correlation = CorrelationId.New();

    protected EventFeatureExtractor _extractor;

    void Establish() => _extractor = new(new TimeBucketResolver());

    protected static AppendedEvent AnEvent(
        Identity? causedBy = null,
        IEnumerable<Causation>? causation = null,
        EventSourceType? eventSourceType = null,
        DateTimeOffset? occurred = null) =>
        new(
            new EventContext(
                new EventType("ExpenseReportApproved", EventTypeGeneration.First),
                eventSourceType ?? EventSourceType.Default,
                "expense-1",
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                occurred ?? Occurred,
                "some-store",
                EventStoreNamespaceName.Default,
                Correlation,
                causation ?? [],
                causedBy ?? Identity.NotSet,
                [],
                EventHash.NotSet),
            new ExpandoObject());
}
