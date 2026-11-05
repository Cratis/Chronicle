// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_entering : given.a_delivery_context
{
    ObserverDeliveryContext _seenByScope;
    ObserverDeliveryContext _current;
    ObserverDelivery _delivery;

    void Because()
    {
        _delivery = ObserverDelivery.Enter(ProviderOf(RecordingScope("a", c => _seenByScope = c)), _context);
        _current = ObserverDeliveryContext.Current;
    }

    void Destroy() => _delivery.Dispose();

    [Fact] void should_publish_the_context() => _current.ShouldEqual(_context);
    [Fact] void should_give_the_context_to_the_scope() => _seenByScope.ShouldEqual(_context);
}
