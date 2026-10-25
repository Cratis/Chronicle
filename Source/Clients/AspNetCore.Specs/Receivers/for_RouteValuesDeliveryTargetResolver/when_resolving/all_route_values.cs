// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_RouteValuesDeliveryTargetResolver.when_resolving;

public class all_route_values : for_MapChronicleReceiver.given.a_receiver_host
{
    protected override string Namespace => "tenant";

    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["eventStore"] = "store";
        context.Request.RouteValues["namespace"] = "tenant";
        context.Request.RouteValues["observerType"] = "ReDuCeR";
        context.Request.RouteValues["observerId"] = "handler";
        await InvokeMappedReceiver(context);
    }
    [Fact] void should_resolve_the_store() => _target.EventStore.Value.ShouldEqual("store");
    [Fact] void should_resolve_the_namespace() => _target.Namespace.Value.ShouldEqual("tenant");
    [Fact] void should_resolve_the_observer_type_case_insensitively() => _target.ObserverType.ShouldEqual(ObserverType.Reducer);
    [Fact] void should_resolve_the_observer_id() => _target.ObserverId.ShouldEqual("handler");
}
