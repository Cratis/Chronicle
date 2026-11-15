// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

public class an_operator_addressing_two_lobbies(ChronicleFixture fixture) : Specification(fixture)
{
    protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);
    protected IEventStore Operator;
    protected IEventStore LobbyA;
    protected IEventStore LobbyB;
    string _suffix;
    string _namespace;

    public override IEnumerable<Type> EventTypes => [typeof(SubscriptionHistoryPublished)];

    async Task Establish()
    {
        _suffix = Guid.NewGuid().ToString("N")[..8];
        _namespace = $"audience-{_suffix}";
        Operator = await ChronicleClient.GetEventStore($"operator-{_suffix}", _namespace);
        LobbyA = await ChronicleClient.GetEventStore($"lobby-a-{_suffix}", _namespace);
        LobbyB = await ChronicleClient.GetEventStore($"lobby-b-{_suffix}", _namespace);
        await Operator.RegisterAll();
        await LobbyA.RegisterAll();
        await LobbyB.RegisterAll();
        foreach (var lobby in new[] { LobbyA, LobbyB })
        {
            // A receiving reactor on the inbox keeps the forwarding observer connected, as in a real lobby.
            var reactor = await lobby.Reactors.Register($"operator-receipts-{lobby.Name}", builder => builder
                .On<SubscriptionHistoryPublished>(_ => { })
                .OnEventSequence($"{EventSequenceId.InboxPrefix}{Operator.Name}"));
            await reactor.WaitTillActive(Timeout);
        }

        await LobbyA.Subscriptions.Subscribe("operator", Operator.Name, builder => builder.WithEventType<SubscriptionHistoryPublished>().ForAudience("lobby-a"));
        await LobbyB.Subscriptions.Subscribe("operator", Operator.Name, builder => builder.WithEventType<SubscriptionHistoryPublished>().ForAudience("lobby-b"));
    }

    protected async Task<ulong> Publish(string value, params string[] audiences)
    {
        var scopes = audiences.Select(EventTags.Audience).ToArray();
        try
        {
            var result = await Operator.GetEventSequence(EventSequenceId.Outbox).Append(value, new SubscriptionHistoryPublished(value));
            result.IsSuccess.ShouldBeTrue();
            return result.SequenceNumber.Value;
        }
        finally
        {
            foreach (var scope in scopes.Reverse()) scope.Dispose();
        }
    }

    protected async Task<IEnumerable<string>> InboxOf(IEventStore lobby, ulong lastOutboxSequenceNumber)
    {
        var key = new Concepts.Observation.ObserverKey($"operator->{lobby.Name}", Operator.Name.Value, _namespace, Concepts.EventSequences.EventSequenceId.Outbox);
        var forwarder = Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(key.ToString());
        using var cancellation = new CancellationTokenSource(Timeout);
        while ((await forwarder.GetState()).LastHandledEventSequenceNumber.Value != lastOutboxSequenceNumber)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
        }

        var events = await lobby.GetEventSequence($"{EventSequenceId.InboxPrefix}{Operator.Name}").GetFromSequenceNumber(0);
        return events.Select(_ => ((SubscriptionHistoryPublished)_.Content).Value).ToArray();
    }
}
