// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexPoisonTracker;

[PerFactSpecificationLifecycle]
public class when_observing_failures : Specification
{
    readonly ConstraintIndexPoisonTracker _tracker = new(new InMemoryStorageErrorClassifier());

    [Fact] void should_require_five_identical_attempts() => Observe(4).ShouldBeEmpty();
    [Fact] void should_settle_the_fifth_attempt() => Observe(5).Keys.ShouldContain((ConstraintName)"unique");
    [Fact] void should_require_other_successful_storage_calls() => Observe(6, storageSucceeded: false).ShouldBeEmpty();
    [Fact] void should_not_settle_a_failure_without_an_event_position() => Observe(6, eventIdentified: false).ShouldBeEmpty();
    [Fact] void should_not_settle_cancellation() => Observe(6, error: new OperationCanceledException()).ShouldBeEmpty();
    [Fact] void should_not_settle_timeout() => Observe(6, error: new TimeoutException()).ShouldBeEmpty();

    [Fact]
    void should_restart_the_count_for_another_exception_type()
    {
        Observe(4);
        Observe(1, error: new InvalidOperationException());
        Observe(4).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_another_event()
    {
        Observe(4);
        Observe(1, position: 2);
        Observe(4).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_after_a_successful_attempt()
    {
        Observe(4);
        _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure>(), true);
        Observe(4).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_a_different_provider_number()
    {
        Observe(4, error: new NumberedFailure(100));
        Observe(1, error: new NumberedFailure(200)).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_a_different_provider_code()
    {
        Observe(4, error: new CodedFailure(100));
        Observe(1, error: new CodedFailure(200)).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_a_different_sql_state()
    {
        Observe(4, error: new StatefulFailure("first"));
        Observe(1, error: new StatefulFailure("second")).ShouldBeEmpty();
    }

    [Fact]
    void should_settle_the_same_failure_despite_changing_messages()
    {
        Observe(4, error: new FormatException("request 1"));
        Observe(1, error: new FormatException("request 2")).Keys.ShouldContain((ConstraintName)"unique");
    }

    [Fact]
    void should_restart_the_count_for_a_different_hresult()
    {
        Observe(4, error: new ResultFailure(100));
        Observe(1, error: new ResultFailure(200)).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_a_provider_code_beneath_a_wrapper()
    {
        Observe(4, error: new InvalidOperationException("wrapper", new NumberedFailure(100)));
        Observe(1, error: new InvalidOperationException("wrapper", new NumberedFailure(200))).ShouldBeEmpty();
    }

    [Fact]
    void should_find_provider_codes_in_later_aggregate_branches()
    {
        Observe(4, error: new AggregateException(new FormatException(), new InvalidOperationException("wrapper", new CodedFailure(100))));
        Observe(1, error: new AggregateException(new FormatException(), new InvalidOperationException("wrapper", new CodedFailure(200)))).ShouldBeEmpty();
    }

    [Fact]
    void should_find_provider_codes_inside_an_aggregate_beneath_a_wrapper()
    {
        Observe(4, error: new InvalidOperationException("wrapper", new AggregateException(new StatefulFailure("first"))));
        Observe(1, error: new InvalidOperationException("wrapper", new AggregateException(new StatefulFailure("second")))).ShouldBeEmpty();
    }

    [Fact]
    void should_keep_poison_settlements_after_a_replay_attempt_is_discarded()
    {
        Observe(5);
        _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure>(), true);
        _tracker.Settled.ShouldContain(((ConstraintName)"unique", EventSequenceNumber.First));
    }

    [Fact]
    void should_release_poison_settlements_only_after_the_constraint_advances()
    {
        Observe(5);
        _tracker.ForgetSettledBefore("unique", 1);
        _tracker.Settled.ShouldBeEmpty();
    }

    IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> Observe(int attempts, EventSequenceNumber? position = null, Exception? error = null, bool storageSucceeded = true, bool eventIdentified = true)
    {
        IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> result = new Dictionary<ConstraintName, ConstraintIndexReplayFailure>();
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            result = _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure> { ["unique"] = new(eventIdentified ? position ?? EventSequenceNumber.First : null, error ?? new FormatException()) }, storageSucceeded);
        }
        return result;
    }

    sealed class ResultFailure : Exception
    {
        public ResultFailure(int result) : base("provider failure") => HResult = result;
    }

    sealed class NumberedFailure(int number) : Exception("provider failure")
    {
        public int Number => number;
    }

    sealed class CodedFailure(int code) : Exception("provider failure")
    {
        public int Code => code;
    }

    sealed class StatefulFailure(string state) : Exception("provider failure")
    {
        public string SqlState => state;
    }
}
