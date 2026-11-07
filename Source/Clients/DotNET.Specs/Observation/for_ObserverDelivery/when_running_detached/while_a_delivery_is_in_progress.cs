// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery.when_running_detached;

public class while_a_delivery_is_in_progress : given.a_delivery_context
{
    ObserverDeliveryContext _seenByWork;
    ObserverDeliveryContext _seenByCaller;

    async Task Because()
    {
        using var delivery = ObserverDeliveryScope.Enter(ProviderOf(), _context);
        await ObserverDeliveryScope.RunDetached(() =>
        {
            _seenByWork = ObserverDeliveryContext.Current;
            return Task.CompletedTask;
        });
        _seenByCaller = ObserverDeliveryContext.Current;
    }

    [Fact] void should_not_give_the_context_to_the_work() => _seenByWork.ShouldBeNull();
    [Fact] void should_keep_the_context_for_the_caller() => _seenByCaller.ShouldEqual(_context);
}
