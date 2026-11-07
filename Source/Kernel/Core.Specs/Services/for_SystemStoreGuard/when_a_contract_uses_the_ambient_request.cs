// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Services.given;
using Cratis.Chronicle.Storage;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_contract_uses_the_ambient_request
{
    [Fact]
    public void should_preserve_both_original_method_signatures()
    {
        typeof(IConstraints).GetMethod(nameof(IConstraints.Register)).GetParameters().Length.ShouldEqual(1);
        typeof(IEventStoreSubscriptions).GetMethod(nameof(IEventStoreSubscriptions.GetSubscriptions)).GetParameters().Length.ShouldEqual(1);
    }

    [Fact]
    public async Task should_refuse_constraints_using_the_current_request()
    {
        using var caller = new an_entitled_caller(ns: "ns");
        var grains = Substitute.For<IGrainFactory>();
        var service = new Events.Constraints.Constraints(grains, new HttpContextAccessor { HttpContext = caller.Http });
        var error = await Catch.Exception(() => service.Register(new RegisterConstraintsRequest { EventStore = "store" }));
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        grains.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_subscriptions_using_the_current_request()
    {
        using var caller = new an_entitled_caller(ns: "ns");
        var storage = Substitute.For<IStorage>();
        var service = new Observation.EventStoreSubscriptions.EventStoreSubscriptions(Substitute.For<IGrainFactory>(), storage, Options.Create(new ChronicleOptions()), new HttpContextAccessor { HttpContext = caller.Http });
        var error = await Catch.Exception(() => service.GetSubscriptions(new GetEventStoreSubscriptionsRequest { TargetEventStore = "store" }));
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        storage.ReceivedCalls().ShouldBeEmpty();
    }
}
