// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_manually_retrying_a_deferred_partition : given.a_webhook_observer
{
    FailedPartition _failure;
    PartitionRecoveryOutcome _outcome;
    PartitionDeferral _overridden;

    readonly RecordingLogger _recordedLogger = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogger;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _overridden = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now, now.AddHours(1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _overridden);
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition("partition");

    [Fact] void should_log_the_reason_at_information_level() => HasInformationLog("Reason", _overridden.Reason).ShouldBeTrue();
    [Fact] void should_log_the_overridden_deadline() => HasInformationLog("Deadline", _overridden.Until).ShouldBeTrue();

    bool HasInformationLog(string key, object value) => _recordedLogger.Entries.Exists(log =>
        log.Level == LogLevel.Information && log.State.Any(entry => entry.Key == key && Equals(entry.Value, value)));

    sealed class RecordingLogger : ILogger<Observer>
    {
        public List<(LogLevel Level, KeyValuePair<string, object?>[] State)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                Entries.Add((logLevel, values.ToArray()));
            }
        }
    }

    [Fact] void should_clear_the_deferral() => _failure.Deferral.ShouldBeNull();
    [Fact] void should_report_a_started_recovery() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_start_from_the_unhandled_event() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.FromSequenceNumber == new Concepts.Events.EventSequenceNumber(42)));
}
