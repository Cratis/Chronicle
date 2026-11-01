// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

[Collection(BackgroundStartupCollection.Name)]
public class when_clients_register_and_append_while_recovery_is_blocked(BackgroundStartupFixture fixture)
{
    [Fact]
    public async Task should_deliver_each_persisted_input_once_to_each_observer()
    {
        const string observerA = "background-startup-a";
        const string observerB = "background-startup-b";
        var callbacks = new ConcurrentQueue<StartupDeliveryRecorded>();
        var inputs = new List<(string Token, ulong Sequence)>();
        var clients = new List<StartupClient>();
        try
        {
            var original = await Connect();
            var originalHandler = await original.Register(observerA, callbacks);
            await AppendBatch(original, "prefix");
            await originalHandler.WaitTillReachesEventSequenceNumber(new(inputs[^1].Sequence), BackgroundStartupFixture.OperationDeadline);
            (await Read(original)).Count(row => row.Content is StartupDeliveryRecorded).ShouldEqual(2);
            var originalConnectionId = original.ConnectionId;
            await original.DisposeAsync();

            var restart = fixture.CrashAndRestart(fixture.EnableBackgroundStartup);
            await fixture.Gate.Entered.WaitAsync(BackgroundStartupFixture.BarrierDeadline);
            await restart.WaitAsync(BackgroundStartupFixture.OperationDeadline);
            fixture.RecoverableWork.IsCompleted.ShouldBeFalse();
            fixture.Gate.IsReleased.ShouldBeFalse();

            var ingress = await Connect();
            await AppendBatch(ingress, "before-registration");
            var reconnected = await Connect();
            var handlerA = await reconnected.Register(observerA, callbacks);
            var newcomer = await Connect();
            var handlerB = await newcomer.Register(observerB, callbacks);
            new[] { originalConnectionId, ingress.ConnectionId, reconnected.ConnectionId, newcomer.ConnectionId }
                .GroupBy(id => id).All(group => group.Count() == 1).ShouldBeTrue();
            await AppendBatch(ingress, "after-registration");
            (await Read(ingress)).Count(row => row.Content is StartupEvent).ShouldEqual(inputs.Count);
            fixture.Gate.IsReleased.ShouldBeFalse();
            fixture.RecoverableWork.IsCompleted.ShouldBeFalse();

            fixture.Gate.Release();
            await fixture.RecoverableWork.WaitAsync(BackgroundStartupFixture.OperationDeadline);
            await AppendBatch(ingress, "final-marker");
            var finalSequence = inputs[^1].Sequence;
            await handlerA.WaitTillReachesEventSequenceNumber(new(finalSequence), BackgroundStartupFixture.OperationDeadline);
            await handlerB.WaitTillReachesEventSequenceNumber(new(finalSequence), BackgroundStartupFixture.OperationDeadline);

            var persisted = await Read(ingress);
            var persistedInputs = persisted.Where(row => row.Content is StartupEvent)
                .Select(row => (((StartupEvent)row.Content).Token, row.Context.SequenceNumber.Value)).ToArray();
            persistedInputs.ShouldEqual(inputs.ToArray());
            var expected = new[] { observerA, observerB }
                .SelectMany(observer => inputs.Select(input => new StartupDeliveryRecorded(observer, input.Token, input.Sequence)))
                .OrderBy(row => row.ObserverId).ThenBy(row => row.InputToken).ToArray();
            callbacks.OrderBy(row => row.ObserverId).ThenBy(row => row.InputToken).ToArray().ShouldEqual(expected);
            persisted.Select(row => row.Content).OfType<StartupDeliveryRecorded>()
                .OrderBy(row => row.ObserverId).ThenBy(row => row.InputToken).ToArray().ShouldEqual(expected);
            var storage = fixture.SiloServices.GetRequiredService<IStorage>()
                .GetEventStore(new(BackgroundStartupFixture.EventStore)).GetNamespace(new(EventStoreNamespaceName.Default.Value));
            foreach (var observer in new[] { observerA, observerB })
            {
                var state = await storage.Observers.Get(new(observer))
                    .WaitAsync(BackgroundStartupFixture.OperationDeadline);
                (state.LastHandledEventSequenceNumber.Value >= finalSequence).ShouldBeTrue();
                state.CatchingUpPartitions.Count.ShouldEqual(0);
                state.ReplayingPartitions.Count.ShouldEqual(0);
                state.FailedPartitions.ShouldBeEmpty();
            }
        }
        finally
        {
            fixture.Gate.Release();
            foreach (var client in clients) await client.DisposeAsync();
        }

        async Task<StartupClient> Connect()
        {
            var client = await StartupClient.Start(fixture.SiloServices);
            clients.Add(client);
            return client;
        }

        async Task AppendBatch(StartupClient client, string batch)
        {
            foreach (var partition in new[] { "one", "two" })
            {
                var token = $"{batch}-{partition}";
                var result = await client.Store.EventLog.Append(partition, new StartupEvent(token))
                    .WaitAsync(BackgroundStartupFixture.OperationDeadline);
                result.IsSuccess.ShouldBeTrue();
                inputs.Add((token, result.SequenceNumber.Value));
            }
        }
    }

    static async Task<AppendedEvent[]> Read(StartupClient client) =>
        (await client.Store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)
            .WaitAsync(BackgroundStartupFixture.OperationDeadline)).ToArray();
}
