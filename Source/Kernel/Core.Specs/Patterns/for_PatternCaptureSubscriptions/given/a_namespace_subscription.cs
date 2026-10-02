// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.given;

public class a_namespace_subscription : Specification
{
    protected TestKitSilo _silo = new();
    protected IPatternCapture _capture;
    protected Func<NamespaceAdded, Task> _onNamespaceAdded;
    protected bool _subscribed;
    protected NamespaceAdded _added = new("some-store", "second-namespace");

    async Task Establish()
    {
        _capture = Substitute.For<IPatternCapture>();
        var subscription = Substitute.For<IBroadcastChannelSubscription>();
        subscription.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>()))
            .Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        _silo.AddService(_capture);
        _silo.AddService(NullLogger<PatternCaptureSubscriptions>.Instance);
        var subscriptions = await _silo.CreateGrainAsync<PatternCaptureSubscriptions>(_added.EventStore);
        await subscriptions.OnSubscribed(subscription);
    }
}
