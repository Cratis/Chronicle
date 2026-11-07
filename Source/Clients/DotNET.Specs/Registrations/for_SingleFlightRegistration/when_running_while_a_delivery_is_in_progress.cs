// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Registrations.for_SingleFlightRegistration;

public class when_running_while_a_delivery_is_in_progress : Specification
{
    ObserverDeliveryContext _seenByAction;

    async Task Because()
    {
        var runner = new SingleFlightRegistration(() =>
        {
            _seenByAction = ObserverDeliveryContext.Current;
            return Task.CompletedTask;
        });
        ObserverDeliveryContext.Current = new("store", "tenant-a", new("observer"), ObserverDeliveryKind.Reactor, typeof(when_running_while_a_delivery_is_in_progress));
        try
        {
            await runner.Run();
        }
        finally
        {
            ObserverDeliveryContext.Current = null;
        }
    }

    [Fact] void should_not_run_the_action_inside_the_delivery() => _seenByAction.ShouldBeNull();
}
