// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Observers.when_registering_a_burst_of_reactors.and_every_reactor_registers_at_once.context;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_burst_of_reactors;

/// <summary>
/// A scaled-down version of a client registering thousands of observers at once: every reactor registers
/// concurrently, none may fail, all must end up subscribed and each must receive an event appended afterwards.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_every_reactor_registers_at_once(context context) : Given<context>(context)
{
    const int ReactorCount = 100;

    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        readonly string _prefix = $"burst-{Guid.NewGuid():N}";
        readonly TaskCompletionSource _allReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _received;

        public int Failed;
        public int Subscribed;
        public int Received;

        public override IEnumerable<Type> EventTypes => [typeof(BurstEvent)];

        async Task Because()
        {
            var timeout = TimeSpanFactory.FromSeconds(120);
            var handlers = await Task.WhenAll(Enumerable.Range(0, ReactorCount).Select(async index =>
            {
                try
                {
                    return await EventStore.Reactors.Register($"{_prefix}-{index}", reactor => reactor.On<BurstEvent>((_, _) =>
                    {
                        if (Interlocked.Increment(ref _received) == ReactorCount)
                        {
                            _allReceived.TrySetResult();
                        }

                        return Task.CompletedTask;
                    }));
                }
                catch
                {
                    Interlocked.Increment(ref Failed);
                    return null;
                }
            }));

            var registered = handlers.OfType<IReactorHandler>().ToArray();
            await Task.WhenAll(registered.Select(handler => handler.WaitTillSubscribed(timeout)));
            Subscribed = registered.Length;

            await EventStore.EventLog.Append("burst-source", new BurstEvent(1));
            await _allReceived.Task.WaitAsync(timeout).ContinueWith(_ => { }, TaskScheduler.Default);
            Received = Volatile.Read(ref _received);
        }

        void Destroy()
        {
            for (var index = 0; index < ReactorCount; index++)
            {
                EventStore.Reactors.Unregister($"{_prefix}-{index}");
            }
        }
    }

    [Fact] void should_not_fail_any_registration() => Context.Failed.ShouldEqual(0);
    [Fact] void should_subscribe_every_reactor() => Context.Subscribed.ShouldEqual(ReactorCount);
    [Fact] void should_deliver_the_event_to_every_reactor() => Context.Received.ShouldEqual(ReactorCount);
}
