// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_resolving_hooks_concurrently : given.a_delivery_context
{
    IServiceProvider _provider;

    void Establish()
    {
        _provider = ProviderOf(RecordingScope("a"));
    }

    void Because() => Parallel.For(0, 64, _ =>
    {
        using var delivery = ObserverDeliveryScope.Enter(_provider, _context);
    });

    [Fact] void should_resolve_the_hooks_once() => _provider.Received(1).GetService(typeof(IEnumerable<IObserverDeliveryScope>));
}
