// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDiagnostics;

public class when_filtering_client_diagnostics : Specification
{
    string _output = null!;

    void Because()
    {
        using var output = new StringWriter();
        using var factory = ClusterLoadDiagnostics.CreateClientLoggerFactory(output);
        var reactor = factory.CreateLogger("Cratis.Chronicle.Reactors.Reactors");
        LoggerMessage.Define(LogLevel.Information, new EventId(8100), "stream-started connection-original")(reactor, null);
        LoggerMessage.Define(LogLevel.Information, new EventId(8101), "terminal\nCancelled")(reactor, null);
        LoggerMessage.Define(LogLevel.Debug, new EventId(8100), "excluded-debug")(reactor, null);
        LoggerMessage.Define(LogLevel.Error, new EventId(8199), "excluded-event")(reactor, new InvalidOperationException("excluded-exception"));
        LoggerMessage.Define(LogLevel.Information, new EventId(8100), "excluded-category")(factory.CreateLogger("Cratis.Chronicle.Connections.ChronicleConnection"), null);
        _output = output.ToString();
    }

    [Fact] public void should_include_the_correlated_lifecycle() => _output.ShouldContain("SQL client stream-started connection-original");
    [Fact] public void should_include_single_line_terminal_evidence() => _output.ShouldContain("SQL client terminal Cancelled");
    [Fact] public void should_exclude_general_logs_and_exception_details() => _output.ShouldNotContain("excluded-");
    [Fact] public void should_emit_only_the_two_safe_events() => _output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldEqual(2);
    [Fact] public void should_use_utc_timestamps() => DateTimeOffset.Parse(_output.Split(' ')[0]).Offset.ShouldEqual(TimeSpan.Zero);
}
