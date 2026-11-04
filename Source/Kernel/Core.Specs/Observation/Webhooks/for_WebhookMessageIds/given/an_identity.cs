// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds.given;

public class an_identity : Specification
{
    protected static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
    protected static readonly ReplayEpoch _otherEpoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"));
    protected WebhookMessageIdentity _identity;

    void Establish() => _identity = Create();

    protected static WebhookMessageIdentity Create(string eventStore = "event-store", string @namespace = "namespace", string sequence = "sequence", ReplayEpoch? epoch = null) =>
        new(
            new EventStoreName(eventStore),
            new EventStoreNamespaceName(@namespace),
            new ObserverId("observer-id"),
            new EventSequenceId(sequence),
            WebhookMessageIdentity.EventsKind,
            [new WebhookMessageBatch((Key)"partition-1", new EventSequenceNumber(42), new EventSequenceNumber(43))],
            epoch);
}
