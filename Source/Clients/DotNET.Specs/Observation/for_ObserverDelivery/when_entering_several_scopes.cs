// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_entering_several_scopes : given.a_delivery_context
{
    void Because()
    {
        using var delivery = ObserverDelivery.Enter(ProviderOf(RecordingScope("a"), RecordingScope("b")), _context);
    }

    [Fact] void should_enter_in_order_and_leave_in_reverse() => _log.ShouldEqual(["enter:a", "enter:b", "leave:b", "leave:a"]);
}
