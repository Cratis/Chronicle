// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_logging_arbitrary_state : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    string _report = null!;
    bool _formatted;

    void Because()
    {
        using var provider = new ClusterLoadDeliveryDiagnosticsLogging(_diagnostics);
        var logger = provider.CreateLogger(ClusterLoadDeliveryDiagnosticsLogging.Category);
        logger.Log(LogLevel.Trace, new EventId(0), "payload-secret", new InvalidOperationException("exception-secret"), (_, _) =>
        {
            _formatted = true;
            throw new InvalidOperationException("formatter must never be called");
        });
        _report = _diagnostics.Describe([], []);
    }

    void Destroy() => _diagnostics.Dispose();

    [Fact] public void should_never_invoke_arbitrary_formatters() => _formatted.ShouldBeFalse();
    [Fact] public void should_not_retain_arbitrary_logger_state() => _report.Contains("secret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] public void should_not_accept_unrecognized_records() => _diagnostics.RetainedRecords.ShouldEqual(0);
}
