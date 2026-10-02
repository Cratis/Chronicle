// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.Hosting.for_ChronicleServerStartupTask.given;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.given;

public class a_namespace_subscription : Specification
{
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
        var subscriptions = new PatternCaptureSubscriptions(_capture, NullLogger<PatternCaptureSubscriptions>.Instance, new an_immediate_time_provider());
        await subscriptions.OnSubscribed(subscription);
    }
}
