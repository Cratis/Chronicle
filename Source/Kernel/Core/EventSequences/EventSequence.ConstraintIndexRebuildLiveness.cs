// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <summary>
    /// The minimum interval between rebuild reporter liveness checks.
    /// </summary>
    internal static readonly TimeSpan ConstraintIndexRebuildLivenessInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The minimum time a rebuild is held before reporting, and between held reports.
    /// </summary>
    internal static readonly TimeSpan ConstraintIndexRebuildHeldReportInterval = TimeSpan.FromMinutes(5);

    static readonly TimeSpan _rebuildStepReadTimeout = TimeSpan.FromSeconds(2);
    readonly Dictionary<ConstraintIndexRebuildRunId, DateTimeOffset> _constraintIndexRebuildHeldReported = [];
    DateTimeOffset _nextConstraintIndexRebuildLivenessCheck;

    enum RebuildStepLiveness
    {
        Alive = 0,
        Ended = 1,
        Undetermined = 2
    }

    IJobStepStorage JobStepStorage => storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).JobSteps;

    static bool IsTerminalConstraintRebuildStep(JobStepStatus status) => status is
        JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure or JobStepStatus.Failed or
        JobStepStatus.Stopped or JobStepStatus.Removing;

    static (RebuildStepLiveness Verdict, string Step) ClassifyStoredRebuildStep(ConstraintIndexRebuildRunId runId, JobStepState? step)
    {
        if (step is null) return (RebuildStepLiveness.Ended, "gone");
        return IsTerminalConstraintRebuildStep(step.Status)
            ? ClassifyTerminalRebuildStep(runId, step)
            : (RebuildStepLiveness.Alive, step.Status.ToString());
    }

    static (RebuildStepLiveness Verdict, string Step) ClassifyTerminalRebuildStep(ConstraintIndexRebuildRunId runId, JobStepState step)
    {
        var terminalChange = step.StatusChanges.LastOrDefault();
        if (terminalChange is null || terminalChange.Status != step.Status)
        {
            return (RebuildStepLiveness.Undetermined, $"{step.Status}; missing or inconsistent status history");
        }
        var stale = runId.Started != default && terminalChange.Occurred < runId.Started;

        // Cratis.Choreography (verified for 1.27.4 and 1.31.0) persists Scheduled before dispatching a retry and the run id is
        // created inside the step after that barrier, so a terminal status left by an earlier attempt cannot pass as this
        // run's; terminal status is written only after work and cancellation handoff drain. The time check is a backstop.
        return (stale ? RebuildStepLiveness.Alive : RebuildStepLiveness.Ended, step.Status.ToString());
    }

    void RecordConstraintIndexRebuildHeld(ConstraintIndexRebuildRunId runId)
    {
        var now = ConstraintIndexTimeProvider.GetUtcNow();
        _constraintIndexRebuildHeldSince.Add(runId, now);
        if (_constraintIndexRebuildRuns.Count == 1)
        {
            _nextConstraintIndexRebuildLivenessCheck = now + ConstraintIndexRebuildLivenessInterval;
        }
    }

    async Task CheckHeldConstraintIndexRebuilds(CancellationToken token)
    {
        var now = ConstraintIndexTimeProvider.GetUtcNow();
        if (_constraintIndexRebuildRuns.Count == 0 || now < _nextConstraintIndexRebuildLivenessCheck) return;
        _nextConstraintIndexRebuildLivenessCheck = now + ConstraintIndexRebuildLivenessInterval;
        foreach (var runId in _constraintIndexRebuildRuns.Keys.ToArray())
        {
            var (verdict, step) = await ClassifyRebuildStep(runId, token);
            if (!_constraintIndexRebuildRuns.TryGetValue(runId, out var names)) continue;
            if (verdict is RebuildStepLiveness.Ended)
            {
                await ReleaseEndedConstraintIndexRebuild(runId, names, step);
            }
            else
            {
                ReportHeldConstraintIndexRebuild(runId, names, now, (verdict, step));
            }
        }
    }

    async Task<(RebuildStepLiveness Verdict, string Step)> ClassifyRebuildStep(ConstraintIndexRebuildRunId runId, CancellationToken token)
    {
        if (runId.JobId is not { } jobId || jobId == JobId.NotSet) return (RebuildStepLiveness.Undetermined, "no job");
        try
        {
            var result = await JobStepStorage.GetForJob(jobId).WaitAsync(_rebuildStepReadTimeout, ConstraintIndexTimeProvider, token);
            if (!result.TryGetResult(out var steps))
            {
                result.TryGetException(out var exception);
                logger.ConstraintIndexRebuildLivenessUnreadable(_eventSequenceKey, runId, exception);
                return (RebuildStepLiveness.Undetermined, "unreadable");
            }
            var step = steps.FirstOrDefault(step => step.Id.JobStepId.Value == runId.Value);
            return ClassifyStoredRebuildStep(runId, step);
        }
        catch (Exception exception)
        {
            logger.ConstraintIndexRebuildLivenessUnreadable(_eventSequenceKey, runId, exception);
            return (RebuildStepLiveness.Undetermined, "unreadable");
        }
    }

    async Task ReleaseEndedConstraintIndexRebuild(ConstraintIndexRebuildRunId runId, ConstraintName[] names, string step)
    {
        logger.ReleasingConstraintIndexRebuildAfterStepEnded(_eventSequenceKey, runId, string.Join(", ", names), step);
        try
        {
            await EndConstraintIndexRebuild(runId, ConstraintIndexRebuildOutcome.ProgressUnknown, ConstraintIndexRebuildHandoffReason.Failed);
        }
        catch (Exception exception)
        {
            logger.ConstraintIndexRebuildReleaseFailed(_eventSequenceKey, runId, exception);
        }
    }

    void ReportHeldConstraintIndexRebuild(ConstraintIndexRebuildRunId runId, ConstraintName[] names, DateTimeOffset now, (RebuildStepLiveness Verdict, string Step) classification)
    {
        var held = now - _constraintIndexRebuildHeldSince[runId];
        if (held < ConstraintIndexRebuildHeldReportInterval) return;
        if (_constraintIndexRebuildHeldReported.TryGetValue(runId, out var reported) && now - reported < ConstraintIndexRebuildHeldReportInterval) return;
        _constraintIndexRebuildHeldReported[runId] = now;
        var level = classification.Verdict is RebuildStepLiveness.Alive ? LogLevel.Information : LogLevel.Warning;
        var detail = classification.Verdict is RebuildStepLiveness.Alive ? classification.Step : $"{classification.Step}; restart the Kernel to recover";
        logger.ConstraintIndexRebuildHeld(level, _eventSequenceKey, runId, string.Join(", ", names), held, detail);
    }
}
