// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_connecting_fails : given.a_chronicle_client_startup
{
    Exception _error;

    void Establish() => _connection.Connect().Returns(_ => Task.FromException(new InvalidOperationException("boom")));

    async Task Because()
    {
        try
        {
            await StartAndSignalStarted();
            await WaitForWarning();
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_warn_that_it_could_not_connect() => _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("Could not connect"));

    async Task WaitForWarning()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            lock (_logger.Entries)
            {
                if (_logger.Entries.Exists(e => e.Level == LogLevel.Warning))
                {
                    return;
                }
            }

            await Task.Delay(10);
        }
    }
}
