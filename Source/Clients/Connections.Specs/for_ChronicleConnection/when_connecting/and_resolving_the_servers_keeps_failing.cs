// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_resolving_the_servers_keeps_failing : given.a_connection_whose_servers_cannot_be_resolved
{
    protected override Exception ResolutionFailure => new NoSrvRecordsFound("_chronicle._tcp.chronicle.example.local");

    async Task Because()
    {
        await _connection.Connect();

        // What the watchdog does in the background, for as long as the servers cannot be resolved.
        for (var attempt = 1; attempt < ChronicleConnection.ResolutionFailuresBeforeError; attempt++)
        {
            await Catch.Exception(_connection.Reconnect);
        }
    }

    [Fact] void should_resolve_again_on_every_attempt() => _resolveAttempts.ShouldBeGreaterThanOrEqual(ChronicleConnection.ResolutionFailuresBeforeError);
    [Fact] void should_log_the_lasting_failure_as_one_error() => _logger.AtLevel(LogLevel.Error).Count().ShouldEqual(1);
    [Fact] void should_point_at_the_name_in_the_connection_string() => _logger.AtLevel(LogLevel.Error).Single().Message.Contains("spelled correctly").ShouldBeTrue();
    [Fact] void should_not_report_the_connection_as_connected() => _lifecycle.DidNotReceive().Connected();
}
