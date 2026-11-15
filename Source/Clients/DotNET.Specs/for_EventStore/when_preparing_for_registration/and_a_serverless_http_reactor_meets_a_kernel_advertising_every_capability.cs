// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Serverless;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

/// <summary>
/// HTTP delivery fails closed whatever the kernel advertises. Only a deliberate client change, together with a kernel
/// that drains replay and recovery state first, can open it, so no stream observer is ever moved to HTTP here.
/// </summary>
public class and_a_serverless_http_reactor_meets_a_kernel_advertising_every_capability : given.an_explicit_store
{
    Exception _error;

    void Establish()
    {
        var capabilities = typeof(KernelCapabilities)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(_ => _.IsLiteral && _.FieldType == typeof(string))
            .Select(_ => (string)_.GetRawConstantValue()!)
            .Append("http-delivery")
            .ToArray();
        var kernel = (IKernelCapabilities)_connection;
        kernel.Capabilities.Returns(capabilities);
        kernel.CapabilitiesAreKnown.Returns(true);

        _options.UseServerless("orders-functions", new Uri("https://orders.example.com"));
        _options.Serverless.DefaultDelivery = ObserverDelivery.Stream;
        _options.Serverless.Observers["http-orders"] = new() { Delivery = ObserverDelivery.Http };
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _options.ExplicitArtifacts.RegisterReactor("http-orders", _ => _.On<PlainEvent>(_ => { }));
        _store = CreateStore();
    }

    async Task Because() => _error = await Catch.Exception(_store.DiscoverAll);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<HttpDeliveryNotAvailable>();
    [Fact] void should_name_the_http_reactor() => _error.Message.ShouldContain("http-orders");
    [Fact] void should_not_ensure_the_store() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_not_activate_any_artifact() => _activator.ReceivedCalls().ShouldBeEmpty();
}
