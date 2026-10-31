// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Webhooks;
using context = Cratis.Chronicle.Integration.for_Webhooks.when_registering.while_a_client_reactor_serves_the_observer.context;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_registering;

[Collection(ChronicleCollection.Name)]
public class while_a_client_reactor_serves_the_observer(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.all_dependencies(fixture)
    {
        readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly string ObserverId = $"transport-owner-{Guid.NewGuid():N}";
        public Exception RegistrationException;
        public IEnumerable<WebhookDefinition> StoredWebhooks;

        public async Task Establish()
        {
            var handler = await EventStore.Reactors.Register(ObserverId, reactor => reactor.On<SomeEvent>((@event, _) =>
            {
                if (@event.Number == 1) _first.TrySetResult();
                if (@event.Number == 2) _second.TrySetResult();
                return Task.CompletedTask;
            }));
            await handler.WaitTillSubscribed();
            await EventStore.EventLog.Append("the-event-source", new SomeEvent(1));
            await _first.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
        }

        public async Task Because()
        {
            RegistrationException = await Catch.Exception(() => Webhooks.Register(ObserverId, TargetUrl, builder => builder.WithEventType<SomeEvent>()));
            StoredWebhooks = await Webhooks.GetAll();
            await EventStore.EventLog.Append("the-event-source", new SomeEvent(2));
            await _second.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
        }

        void Destroy() => EventStore.Reactors.Unregister(ObserverId);
    }

    [Fact] void should_return_without_throwing() => Context.RegistrationException.ShouldBeNull();
    [Fact] void should_not_store_the_refused_webhook() => Context.StoredWebhooks.Any(webhook => webhook.Identifier.Value == Context.ObserverId).ShouldBeFalse();
    [Fact] void should_not_deliver_to_the_webhook() => Context.InvokedWebhooks.Count.ShouldEqual(0);
}
