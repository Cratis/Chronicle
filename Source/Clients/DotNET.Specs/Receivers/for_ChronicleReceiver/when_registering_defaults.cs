// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver;

public class when_registering_defaults : Specification
{
    ServiceProvider _provider;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IChronicleClient>());
        services.AddSingleton<IEventStoreNamespaceResolver, DefaultEventStoreNamespaceResolver>();
        services.AddChronicleReceiver(_ => new ChronicleOptions());
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_resolve_the_receiver() => _provider.GetRequiredService<IChronicleReceiver>().ShouldBeOfExactType<ChronicleReceiver>();
    [Fact] void should_resolve_the_webhook_decoder() => _provider.GetRequiredService<IDeliveryDecoder>().ShouldBeOfExactType<WebhookBodyDecoder>();
    [Fact] void should_use_the_provisional_status_mapping() => _provider.GetRequiredService<IDeliveryStatusCodes>().ShouldBeOfExactType<ProvisionalDeliveryStatusCodes>();
    [Fact] void should_share_the_receiver() => _provider.GetRequiredService<IChronicleReceiver>().ShouldEqual(_provider.GetRequiredService<IChronicleReceiver>());
    void Destroy() => _provider?.Dispose();
}
