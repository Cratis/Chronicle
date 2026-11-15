// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_the_silo_stops : given.a_retry_loop
{
    LogLevel _completion;

    async Task Because()
    {
        _retries.Defer(_eventStore);
        var timer = await _timeProvider.Next();
        _retries.Stop();
        _completion = await _logger.Next();
        timer.Complete();
        _retries.Defer("another-store");
    }

    [Fact] async Task should_not_register_after_stopping() => await _kernelProjections.DidNotReceiveWithAnyArgs().DiscoverAndRegister(default!);
    [Fact] void should_finish_without_an_error() => _completion.ShouldEqual(LogLevel.Information);
    [Fact] void should_not_schedule_another_retry() => _timeProvider.Delays.Count.ShouldEqual(1);
}
