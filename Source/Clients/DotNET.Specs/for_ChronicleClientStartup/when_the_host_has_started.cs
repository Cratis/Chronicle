// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_the_host_has_started : given.a_chronicle_client_startup
{
    async Task Because()
    {
        await StartAndSignalStarted();
        await _connectCalled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_connect_the_event_store() => _connection.Received(1).Connect();
    [Fact] void should_log_the_event_store() => _logger.Entries.ShouldContain(e => e.Message.Contains("the-store"));
    [Fact] void should_not_warn() => _logger.Entries.ShouldNotContain(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
}
