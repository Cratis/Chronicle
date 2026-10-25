// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_RouteValuesDeliveryTargetResolver.when_resolving;

public class an_unknown_event_store : for_MapChronicleReceiver.given.a_receiver_host
{
    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["eventStore"] = "unknown";
        context.Request.RouteValues["namespace"] = "tenant-a";
        context.Request.RouteValues["observerType"] = "reactor";
        context.Request.RouteValues["observerId"] = "handler";
        await InvokeMappedReceiver(context);
    }
    [Fact] void should_refuse_the_target() => _target.ShouldBeNull();
    [Fact] void should_forward_the_request_to_the_receiver() => _received.Task.IsCompleted.ShouldBeTrue();
}
