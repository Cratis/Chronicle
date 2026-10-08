// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader.when_stopping;

public class and_the_read_in_flight_can_also_be_cancelled_by_the_caller : given.a_reader_with_a_read_in_flight
{
    readonly CancellationTokenSource _caller = new();
    Exception _error;

    protected override CancellationToken CallerCancellation => _caller.Token;

    async Task Because() => _error = await Catch.Exception(_reader.Stop);

    [Fact] void should_cancel_the_read_in_flight() => _readCancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_have_waited_for_the_read_in_flight() => _readInFlight.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_cancel_the_callers_token() => _caller.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_fail() => _error.ShouldBeNull();

    void Destroy() => _caller.Dispose();
}
