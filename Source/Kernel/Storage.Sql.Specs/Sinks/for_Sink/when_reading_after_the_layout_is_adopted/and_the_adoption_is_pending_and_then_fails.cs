// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_after_the_layout_is_adopted;

public class and_the_adoption_is_pending_and_then_fails : given_a_sink_that_needs_its_layout
{
    Exception? _adoptionFailure;
    int _opensBeforeSecondRead;
    int _opensAfterSecondRead;

    protected override async Task OnTableOpening(int ordinal)
    {
        if (ordinal == 1)
        {
            _adoptionReached.SetResult();
            await _adoptionGate.Task;
        }
    }

    async Task Because()
    {
        var adoption = AdoptLayout();
        await _adoptionReached.Task;
        await _sink.GetInstances();

        _adoptionGate.SetException(new InvalidOperationException("The adoption failed."));
        await adoption.ContinueWith(_ => { }, TaskScheduler.Default);
        _adoptionFailure = adoption.Exception?.GetBaseException();

        _opensBeforeSecondRead = _harness.ExistingTableOpens;
        await _sink.GetInstances();
        _opensAfterSecondRead = _harness.ExistingTableOpens;
    }

    [Fact] void should_fail_the_adoption() => _adoptionFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_resolve_the_layout_again_on_the_next_read() => _opensAfterSecondRead.ShouldEqual(_opensBeforeSecondRead + 1);
}
