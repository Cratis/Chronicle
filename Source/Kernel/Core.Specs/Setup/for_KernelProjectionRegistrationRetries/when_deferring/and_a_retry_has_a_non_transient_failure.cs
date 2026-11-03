// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_a_retry_has_a_non_transient_failure : given.a_retry_loop
{
    LogLevel _level;

    void Establish() => _kernelProjections.DiscoverAndRegister(_eventStore).Returns(_ => Task.FromException(new InvalidOperationException()));

    async Task Because()
    {
        _retries.Defer(_eventStore);
        (await _timeProvider.Next()).Complete();
        _level = await _logger.Next();
        await _timeProvider.Next();
    }

    [Fact] void should_report_the_defect() => _level.ShouldEqual(LogLevel.Error);
    [Fact] void should_keep_the_store_pending() => _retries.Pending.ShouldContain(_eventStore);
    [Fact] void should_keep_retrying() => _timeProvider.Delays.Count.ShouldEqual(2);
}
