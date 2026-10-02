// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions;

public class when_a_namespace_is_added : Specification
{
    IPatternCapture _capture;
    Func<NamespaceAdded, Task> _onNamespaceAdded;

    async Task Establish()
    {
        _capture = Substitute.For<IPatternCapture>();
        var subscription = Substitute.For<IBroadcastChannelSubscription>();
        subscription.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>()))
            .Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        var subscriptions = new PatternCaptureSubscriptions(_capture);
        await subscriptions.OnSubscribed(subscription);
    }

    Task Because() => _onNamespaceAdded(new NamespaceAdded("some-store", "second-namespace"));

    [Fact] async Task should_subscribe_capture_in_the_added_namespace() => await _capture.Received(1).Subscribe("some-store", "second-namespace");
}
