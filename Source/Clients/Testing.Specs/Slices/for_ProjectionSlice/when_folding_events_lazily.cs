// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_folding_events_lazily : a_projection_slice
{
    ProjectionSliceOutcome<Tally> _outcome;
    int _before;
    Exception _repeat;
    Exception _late;
    Exception _callback;
    Exception _original;

    void Establish()
    {
        object[] events = [new Tallied()];
        _slice.GivenEvents(_source, events).GivenEvents(_source, new Tallied());
        events[0] = new TallyBroke();
        _before = _scenario.Instances.Count;
    }

    async Task Because()
    {
        _outcome = await _slice.Run();
        _outcome.ShouldPartiallyMatchReadModel(_source, new { Count = 2 })
            .ShouldExactlyMatchReadModel(_source, new { Id = Guid.Empty, Count = 2 })
            .ShouldHaveNoReadModel("missing");
        _repeat = await Catch.Exception(_slice.Run);
        _late = Catch.Exception(() => _slice.GivenEvents(_source, new Tallied()));
        _original = new InvalidOperationException("Callback assertion failed.");
        _callback = Catch.Exception(() => _outcome.Should(_ => throw _original));
    }

    [Fact] void should_defer_collection_until_run() => _before.ShouldEqual(0);
    [Fact] void should_copy_the_supplied_array_and_fold_all_batches() => _scenario.Instance!.Count.ShouldEqual(2);
    [Fact] void should_expose_the_successful_outcome() => ReferenceEquals(_slice.Then, _outcome).ShouldBeTrue();
    [Fact] void should_reject_repeat_execution() => _repeat.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] void should_reject_late_inputs() => _late.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] void should_propagate_the_callback_exception_without_wrapping() => ReferenceEquals(_original, _callback).ShouldBeTrue();
}
