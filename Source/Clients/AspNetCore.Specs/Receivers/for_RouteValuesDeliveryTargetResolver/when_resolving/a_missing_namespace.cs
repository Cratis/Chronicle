// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_RouteValuesDeliveryTargetResolver.when_resolving;

public class a_missing_namespace : for_MapChronicleReceiver.given.a_receiver_host
{
    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["eventStore"] = "store";
        context.Request.RouteValues["observerType"] = "reactor";
        context.Request.RouteValues["observerId"] = "handler";
        context.Request.QueryString = new("?namespace=not-a-route-value");
        await InvokeMappedReceiver(context);
    }
    [Fact] void should_forward_the_delivery_to_the_receiver() => _received.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_infer_the_namespace_from_the_query() => _target.ShouldBeNull();
}
