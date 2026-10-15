// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_namespace_is_added;

public class with_pinned_generations : given.a_manager_with_pinned_generations
{
    Func<NamespaceAdded, Task> _onNamespaceAdded;
    async Task Establish()
    {
        var channel = Substitute.For<IBroadcastChannelSubscription>();
        channel.ChannelId.Returns(ChannelId.Create("namespaces", "Lobby"));
        channel.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>())).Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        await _manager.OnSubscribed(channel);
    }
    async Task Because() => await _onNamespaceAdded(new("Lobby", EventStoreNamespaceName.Default));
    [Fact] void should_subscribe_with_the_persisted_pins() => ShouldSubscribePinned();
}
