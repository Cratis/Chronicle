// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class and_the_occurrence_is_application_scoped_after_a_delivery_context_was_inherited : given.an_attempt_with_a_delivery_hook
{
    ObserverDeliveryContext _inherited;
    ObserverDeliveryContext _afterwards;

    async Task Because()
    {
        _inherited = new("store", "tenant-a", new("observer"), ObserverDeliveryKind.Reactor, typeof(and_the_occurrence_is_application_scoped_after_a_delivery_context_was_inherited));
        ObserverDeliveryContext.Current = _inherited;
        try
        {
            await Run(new(true, null));
            _afterwards = ObserverDeliveryContext.Current;
        }
        finally
        {
            ObserverDeliveryContext.Current = null;
        }
    }

    [Fact] void should_run_the_handler_outside_any_delivery() => _log.ShouldEqual(["activated:"]);
    [Fact] void should_restore_the_context_of_the_caller() => _afterwards.ShouldEqual(_inherited);
}
