// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_entering_without_any_scope_registered : given.a_delivery_context
{
    ObserverDeliveryContext _withDefaultProvider;
    ObserverDeliveryContext _withNullProvider;

    void Because()
    {
        using (ObserverDelivery.Enter(new DefaultServiceProvider(), _context))
        {
            _withDefaultProvider = ObserverDeliveryContext.Current;
        }

        using (ObserverDelivery.Enter(Substitute.For<IServiceProvider>(), _context))
        {
            _withNullProvider = ObserverDeliveryContext.Current;
        }
    }

    [Fact] void should_publish_the_context_with_the_default_provider() => _withDefaultProvider.ShouldEqual(_context);
    [Fact] void should_publish_the_context_with_a_provider_returning_null() => _withNullProvider.ShouldEqual(_context);
}
