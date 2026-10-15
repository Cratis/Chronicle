// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.TestKit;

namespace Cratis.Chronicle.Captures.for_CaptureEventsNamespaceSubscriptions.given;

public class a_namespace_subscription : Specification
{
    protected TestKitSilo _silo = new();
    protected ICapturesManager _manager;
    protected Func<NamespaceAdded, Task> _onNamespaceAdded;
    protected NamespaceAdded _added = new("some-store", "second-namespace");

    async Task Establish()
    {
        _manager = Substitute.For<ICapturesManager>();
        var subscription = Substitute.For<IBroadcastChannelSubscription>();
        subscription.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>()))
            .Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        _silo.AddProbe(_ => _manager);
        _silo.AddService(NullLogger<CaptureEventsNamespaceSubscriptions>.Instance);
        var subscriptions = await _silo.CreateGrainAsync<CaptureEventsNamespaceSubscriptions>(_added.EventStore.Value);
        await subscriptions.OnSubscribed(subscription);
    }
}
