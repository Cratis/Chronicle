// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ComposedStartup;

public class when_a_schema_lock_is_not_acquired : given.a_schema_lock_startup_step
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(-3, false)]
    [InlineData(-1, true)]
    [InlineData(-3, true)]
    async Task should_retry_transient_contention_until_the_step_succeeds(int result, bool wrapped)
    {
        _resultForAttempt = attempt => attempt == 1 ? result : 0;
        _wrapFailure = wrapped;
        var error = await Catch.Exception(Execute);
        error.ShouldBeNull();
        _attempts.ShouldEqual(2);
        _time.Delays.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-3)]
    async Task should_surface_transient_contention_after_the_existing_budget_is_exhausted(int result)
    {
        _resultForAttempt = _ => result;
        var error = await Catch.Exception(Execute);
        error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
        _attempts.ShouldEqual(5);
        _time.Delays.ShouldEqual([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)]);
    }

    [Theory]
    [InlineData(-2, false)]
    [InlineData(-999, false)]
    [InlineData(-4, false)]
    [InlineData(-2, true)]
    [InlineData(-999, true)]
    async Task should_fail_immediately_for_canceled_or_invalid_lock_requests(int result, bool wrapped)
    {
        _resultForAttempt = _ => result;
        _wrapFailure = wrapped;
        var error = await Catch.Exception(Execute);
        if (wrapped)
        {
            error.ShouldBeOfExactType<InvalidOperationException>();
            error.InnerException.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
        }
        else
        {
            error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
        }
        _attempts.ShouldEqual(1);
        _time.Delays.ShouldBeEmpty();
    }

    [Fact]
    async Task should_stop_startup_retries_on_actual_caller_cancellation()
    {
        _cancelDuringAcquire = true;
        var error = await Catch.Exception(Execute);
        (error is OperationCanceledException).ShouldBeTrue();
        _attempts.ShouldEqual(1);
        _time.Delays.ShouldBeEmpty();
    }
}
