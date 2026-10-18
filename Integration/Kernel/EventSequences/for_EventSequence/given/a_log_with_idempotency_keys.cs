// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Registrations;

using KernelAppendResult = Cratis.Chronicle.EventSequences.IdempotentAppendResult;
using KernelEventToAppend = Cratis.Chronicle.EventSequences.EventToAppend;
using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class a_log_with_idempotency_keys(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    protected KernelEventToAppend _request;
    protected IdempotencyKey _key;
    public KernelAppendResult First;
    public KernelAppendResult Retry;
    public KernelAppendResult[] Racing;
    public ulong Count;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

    async Task Establish()
    {
        await EventStore.DiscoverAll();
        await EventStore.RegisterAll();
        (await EventStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _key = new($"integration-{Guid.NewGuid():N}");
        var type = EventStore.EventTypes.GetEventTypeFor(typeof(CustomerNamed));
        _request = new(
            EventSourceType.Default,
            $"customer-{Guid.NewGuid():N}",
            EventStreamType.All,
            EventStreamId.Default,
            new KernelEventType(type.Id.Value, type.Generation.Value),
            [],
            new JsonObject { [nameof(CustomerNamed.Name)] = "Alice" });
    }

    protected Task<KernelAppendResult> Append() => ((IChronicleSetupFixture)this).EventLogSequenceGrain.AppendWithIdempotencyKey(_request, _key, CorrelationId.New(), [], Identity.System, ConcurrencyScope.None);

    protected async Task CountEvents() => Count = (await ((IChronicleSetupFixture)this).GetEventLogStorage().GetCount(eventTypes: [_request.EventType])).Value;
}
