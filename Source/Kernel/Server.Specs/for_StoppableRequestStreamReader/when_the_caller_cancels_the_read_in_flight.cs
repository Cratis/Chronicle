// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader;

public class when_the_caller_cancels_the_read_in_flight : given.a_reader_with_a_read_in_flight
{
    readonly CancellationTokenSource _caller = new();

    protected override CancellationToken CallerCancellation => _caller.Token;

    async Task Because() => await _caller.CancelAsync();

    [Fact] void should_cancel_the_read_in_flight() => _readCancellation.IsCancellationRequested.ShouldBeTrue();

    void Destroy() => _caller.Dispose();
}
