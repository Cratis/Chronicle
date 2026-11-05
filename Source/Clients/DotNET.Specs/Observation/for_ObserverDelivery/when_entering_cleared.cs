// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_entering_cleared : given.a_delivery_context
{
    ObserverDeliveryContext _cleared;
    ObserverDeliveryContext _restored;

    void Because()
    {
        using (ObserverDelivery.Enter(ProviderOf(), _context))
        {
            using (ObserverDelivery.EnterCleared())
            {
                _cleared = ObserverDeliveryContext.Current;
            }

            _restored = ObserverDeliveryContext.Current;
        }
    }

    [Fact] void should_have_no_context_while_cleared() => _cleared.ShouldBeNull();
    [Fact] void should_restore_the_previous_context() => _restored.ShouldEqual(_context);
}
