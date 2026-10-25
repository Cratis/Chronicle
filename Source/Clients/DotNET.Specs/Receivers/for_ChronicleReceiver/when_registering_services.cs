// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver;

public class when_registering_services : Specification
{
    ServiceProvider _provider;
    IDeliveryDecoder _decoder;
    IDeliveryStatusCodes _codes;
    IChronicleReceiver _receiver;

    void Establish()
    {
        _decoder = Substitute.For<IDeliveryDecoder>();
        _codes = Substitute.For<IDeliveryStatusCodes>();
        _receiver = Substitute.For<IChronicleReceiver>();
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_decoder);
        services.AddSingleton(_codes);
        services.AddSingleton(_receiver);
        services.AddChronicleReceiver(_ => throw new InvalidOperationException("An overridden decoder should not resolve options."));
        _provider = services.BuildServiceProvider();
    }

    [Fact] void should_preserve_the_hosts_decoder() => _provider.GetRequiredService<IDeliveryDecoder>().ShouldEqual(_decoder);
    [Fact] void should_preserve_the_hosts_status_mapping() => _provider.GetRequiredService<IDeliveryStatusCodes>().ShouldEqual(_codes);
    [Fact] void should_preserve_the_hosts_receiver() => _provider.GetRequiredService<IChronicleReceiver>().ShouldEqual(_receiver);
    void Destroy() => _provider?.Dispose();
}
