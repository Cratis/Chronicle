// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_leaving : given.a_delivery_context
{
    ObserverDeliveryContext _innerCurrent;
    ObserverDeliveryContext _afterInner;
    ObserverDeliveryContext _afterOuter;

    void Because()
    {
        var provider = ProviderOf();
        var inner = _context with { ObserverId = new("inner") };
        var outer = ObserverDeliveryScope.Enter(provider, _context);
        var nested = ObserverDeliveryScope.Enter(provider, inner);
        _innerCurrent = ObserverDeliveryContext.Current;
        nested.Dispose();
        _afterInner = ObserverDeliveryContext.Current;
        outer.Dispose();
        _afterOuter = ObserverDeliveryContext.Current;
    }

    [Fact] void should_have_published_the_nested_context() => _innerCurrent.ObserverId.Value.ShouldEqual("inner");
    [Fact] void should_restore_the_outer_context_after_the_nested_one() => _afterInner.ShouldEqual(_context);
    [Fact] void should_restore_no_context_after_the_outer_one() => _afterOuter.ShouldBeNull();
}
