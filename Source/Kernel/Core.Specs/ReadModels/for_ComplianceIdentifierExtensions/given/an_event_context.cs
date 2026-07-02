// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.given;

public class an_event_context : Specification
{
    protected const string EventSourceIdValue = "event-source-id";
    protected const string ResolvedKeyValue = "resolved-key";
    protected const string ExplicitSubjectValue = "explicit-subject";

    protected static EventContext EventContextFor(string eventSourceId, Subject? subject = null) =>
        EventContext.From(
            "test-store",
            "test-namespace",
            EventType.Unknown,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet,
            subject: subject);

    protected static Key KeyFor(string value) => new(value, ArrayIndexers.NoIndexers);
}
