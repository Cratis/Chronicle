// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader.when_stopping;

public class and_the_read_in_flight_ends_when_cancelled : given.a_reader_with_a_read_in_flight
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_reader.Stop);

    [Fact] void should_cancel_the_read_in_flight() => _readCancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_have_waited_for_the_read_in_flight() => _readInFlight.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
