// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_token_is_cancelled : given.a_keeper
{
    Exception _error;
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => Observer.OnStart(cancellation.Token));
    }

    [Fact] void should_honor_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
}
