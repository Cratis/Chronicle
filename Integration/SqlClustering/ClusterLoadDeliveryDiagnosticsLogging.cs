// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Integration.SqlClustering;

/// <summary>
/// Fixture-owned logger transport. Only the candidate's exact reactor category/schema is accepted.
/// Never calls formatters, ToString, exception formatting, or enumerates arbitrary state.
/// </summary>
/// <param name="diagnostics">The bounded capture.</param>
internal sealed class ClusterLoadDeliveryDiagnosticsLogging(ClusterLoadDeliveryDiagnostics diagnostics) : ILoggerProvider
{
    internal const string Category = "Cratis.Chronicle.Reactors.Reactors";
    const string Format = "RollingDelivery Stage={Stage} Stream={Stream} Batch={Batch} Delivery={Delivery} Sequence={Sequence} Previous={Previous} State={State} Count={Count} Partition={Partition} Type={Type} Source={Source} Correlation={Correlation} Redacted={Redacted}";
    readonly ActivityListener _listener = Listen(diagnostics);

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new Logger(diagnostics, categoryName == Category);

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    static ActivityListener Listen(ClusterLoadDeliveryDiagnostics capture)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ClientActivity.SourceName || source.Name == Category,
            Sample = (ref options) => Sample(options.Name, capture),
            SampleUsingParentId = (ref options) => Sample(options.Name, capture),
            ActivityStarted = activity => RecordSpan(capture, activity, 30),
            ActivityStopped = activity => RecordSpan(capture, activity, 31)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    static ActivitySamplingResult Sample(string operation, ClusterLoadDeliveryDiagnostics capture) => operation == "client.reactor.handle" && capture.HasScope ? ActivitySamplingResult.AllData : ActivitySamplingResult.None;

    static void RecordSpan(ClusterLoadDeliveryDiagnostics capture, Activity activity, int stage)
    {
        if (activity.OperationName == "client.reactor.handle") capture.Span(stage, (int)activity.Status);
    }

    sealed class Logger(ClusterLoadDeliveryDiagnostics capture, bool allowed) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => allowed && logLevel == LogLevel.Trace;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (!allowed) return null;
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values) return null;
            return ReadScope(values);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Capture(logLevel, state);

        IDisposable? ReadScope(IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            if (values.Count != 2) return null;
            if (!Read(values, 0, "RollingStream", out Guid stream)) return null;
            if (!Read(values, 1, "RollingBatch", out long batch)) return null;
            return capture.BeginScope(stream, batch);
        }

        void Capture<TState>(LogLevel logLevel, TState state)
        {
            if (!IsEnabled(logLevel)) return;

            // Generated LoggerMessage state is bounded IReadOnlyList. Reject other shapes without touching them.
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values) return;
            if (values.Count != 14) return;
            if (!IsSchema(values[13])) return;
            if (TryRead(values, out var record)) capture.Record(record);
            else capture.Reject();
        }

        static bool IsSchema(KeyValuePair<string, object?> value)
        {
            if (value.Key != "{OriginalFormat}") return false;
            return value.Value is string format && format == Format;
        }

        static bool TryRead(IReadOnlyList<KeyValuePair<string, object?>> values, out ClusterLoadDeliveryDiagnostics.Evidence evidence)
        {
            evidence = default;

            // Fixed positional schema, exact primitive types only. Never retains the list or its values.
            var valid = Read(values, 0, "Stage", out int stage);
            valid &= Read(values, 1, "Stream", out Guid stream);
            valid &= Read(values, 2, "Batch", out long batch);
            valid &= Read(values, 3, "Delivery", out ulong delivery);
            valid &= Read(values, 4, "Sequence", out ulong sequence);
            valid &= Read(values, 5, "Previous", out ulong previous);
            valid &= Read(values, 6, "State", out int result);
            valid &= Read(values, 7, "Count", out int count);
            valid &= Read(values, 8, "Partition", out Guid partition);
            valid &= Read(values, 9, "Type", out Guid type);
            valid &= Read(values, 10, "Source", out Guid source);
            valid &= Read(values, 11, "Correlation", out Guid correlation);
            valid &= Read(values, 12, "Redacted", out int redacted);
            if (!valid || !ValidRange(stage, count)) return false;
            evidence = new(stage, stream, batch, delivery, sequence, previous, result, count, partition, type, source, correlation, redacted);
            return true;
        }

        static bool ValidRange(int stage, int count) => stage is >= 1 and <= 11 && count >= 0;

        static bool Read<T>(IReadOnlyList<KeyValuePair<string, object?>> values, int index, string key, out T value)
        {
            if (values[index].Key == key && values[index].Value is T typed)
            {
                value = typed;
                return true;
            }
            value = default!;
            return false;
        }
    }
}
