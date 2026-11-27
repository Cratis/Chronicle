// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using ProjectionState = Cratis.Chronicle.Projections.ProjectionState;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Adds a bounded, best effort snapshot of jobs and observers to a replay wait failure, without changing the wait.
/// </summary>
internal static class ReplayWaitDiagnostics
{
    internal const int MaxJobs = 20;
    internal const int MaxIdentifierLength = 128;
    internal const int MaxSectionLength = 2048;
    internal const int MaxSummaryLength = 8192;

    const string TruncatedMarker = "...[truncated]";

    /// <summary>
    /// Runs the wait once; on cancellation, timeout or terminal job failure, throws <see cref="ReplayWaitDidNotComplete"/> with a bounded snapshot.
    /// </summary>
    /// <param name="wait">The original wait.</param>
    /// <param name="sources">Where to read the snapshot from.</param>
    /// <param name="createBudget">Optional factory of the single shared budget for the snapshot reads; defaults to five seconds.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ReplayWaitDidNotComplete">The wait did not finish successfully; the original exception is the inner exception.</exception>
    public static async Task Run(Func<Task> wait, ReplayWaitSources sources, Func<CancellationTokenSource>? createBudget = null)
    {
        try
        {
            await wait();
        }
        catch (Exception original) when (original is OperationCanceledException or TimeoutException or JobFailedWhileWaiting)
        {
            var failed = (original as JobFailedWhileWaiting)?.Job;
            throw new ReplayWaitDidNotComplete(await TakeSnapshot(sources, failed, createBudget ?? DefaultBudget), original);
        }
    }

    static CancellationTokenSource DefaultBudget() => new(TimeSpan.FromSeconds(5));

    static async Task<string> TakeSnapshot(ReplayWaitSources sources, Job? failed, Func<CancellationTokenSource> createBudget)
    {
        try
        {
            using var budget = createBudget();
            var token = budget.Token;
            var sections = await Task.WhenAll(
                DescribeJobs(sources, failed, token),
                Read(async () => DescribeObserver("reactor", sources.ReactorName, await sources.GetReactorState()), token),
                Read(async () => DescribeObserver("reducer", sources.ReducerName, await sources.GetReducerState()), token),
                Read(async () => DescribeObserver("projection", sources.ProjectionName, await sources.GetProjectionState()), token));
            var context = sources.ScenarioSource is { } source ? $"scenario context: source={Sanitize(source)}" : null;
            var summary = context is null ? sections : sections.Take(1).Append(context).Concat(sections.Skip(1));
            return Cap(string.Join(Environment.NewLine, summary.Select(_ => Cap(_, MaxSectionLength))), MaxSummaryLength);
        }
        catch (Exception exception)
        {
            return $"snapshot failed: {exception.GetType().Name}";
        }
    }

    static Task<string> Read(Func<Task<string>> reader, CancellationToken budget) => Read(reader, budget, failure => failure);

    static async Task<T> Read<T>(Func<Task<T>> reader, CancellationToken budget, Func<string, T> onFailure)
    {
        try
        {
            var read = reader();
            _ = read.ContinueWith(static task => task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return await read.WaitAsync(budget);
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested)
        {
            return onFailure("snapshot timed out");
        }
        catch (Exception exception)
        {
            return onFailure($"snapshot failed: {exception.GetType().Name}");
        }
    }

    static async Task<string> DescribeJobs(ReplayWaitSources sources, Job? failed, CancellationToken budget)
    {
        var primary = failed is null ? Task.FromResult<string?>(null) : DescribePrimary();
        var others = DescribePolledJobs(failed?.Id, sources, budget);
        var first = await primary;
        var rest = await others;
        return first is null ? rest : $"jobs: (failure poll){Environment.NewLine}{first}{Environment.NewLine}{rest}";

        async Task<string?> DescribePrimary() => await DescribeJob(failed!, sources, budget);
    }

    static async Task<string> DescribePolledJobs(JobId? excluded, ReplayWaitSources sources, CancellationToken budget)
    {
        var limit = excluded is null ? MaxJobs : MaxJobs - 1;
        var (shown, error) = await Read(
            async () => (Jobs: (await sources.GetJobs()).Where(_ => _.Id != excluded).OrderByDescending(IsFailed).Take(limit + 1).ToArray(), Error: (string?)null),
            budget,
            failure => ([], failure));
        if (error is not null) return $"jobs: {error}";
        if (shown.Length == 0)
        {
            return "jobs: none outstanding";
        }

        var entries = await Task.WhenAll(shown.Take(limit).Select(job => DescribeJob(job, sources, budget)));
        var rows = entries.Prepend("jobs:");
        if (shown.Length > limit)
        {
            rows = rows.Append($"  more jobs omitted {TruncatedMarker}");
        }

        return string.Join(Environment.NewLine, rows);
    }

    static async Task<string> DescribeJob(Job job, ReplayWaitSources sources, CancellationToken budget)
    {
        var progress = job.Progress;
        var row = $"  job {job.Id.Value:D} type={Sanitize(job.Type.Value)} status={job.Status} steps total={progress.TotalSteps} successful={progress.SuccessfulSteps} failed={progress.FailedSteps} stopped={progress.StoppedSteps} isCompleted={progress.IsCompleted} isStopped={progress.IsStopped}";
        if (!IsFailed(job)) return row;

        var scope = Read(async () => sources.GetJobScope is { } lookup && await lookup(job.Id) is { } owner
            ? $"observer={Sanitize(owner.Observer)} partition={Sanitize(owner.Partition)}"
            : "unavailable", budget);
        var steps = Read(async () => DescribeSteps(await (sources.GetJobSteps?.Invoke(job) ?? job.GetJobSteps())), budget);
        return $"{row}{Environment.NewLine}    job scope {await scope}{Environment.NewLine}    job steps: {await steps}";
    }

    static bool IsFailed(Job job) => job.Status is JobStatus.Failed or JobStatus.CompletedWithFailures;

    static string DescribeSteps(IEnumerable<JobStep> steps)
    {
        const int maxSteps = 3;
        var shown = steps.OrderByDescending(_ => IsFailedStep(_.Status)).Take(maxSteps + 1).ToArray();
        if (shown.Length == 0) return "none available";

        var rows = shown.Take(maxSteps).Select(DescribeStep);
        if (shown.Length > maxSteps) rows = rows.Append($"more steps omitted {TruncatedMarker}");
        return string.Join(Environment.NewLine + "    ", rows);
    }

    static string DescribeStep(JobStep step)
    {
        const int maxChanges = 4;
        var ordered = step.StatusChanges.OrderBy(_ => _.Occurred).ToArray();
        var latest = ordered.TakeLast(maxChanges - 1).ToList();
        var terminal = ordered.LastOrDefault(_ => IsFailedStep(_.Status) || _.Status is JobStepStatus.CompletedSuccessfully or JobStepStatus.Stopped);
        var extra = terminal is not null && !latest.Contains(terminal) ? terminal : ordered.SkipLast(latest.Count).LastOrDefault();
        if (extra is not null) latest.Add(extra);

        var history = latest.OrderBy(_ => _.Occurred).Select(_ => $"{_.Occurred:O} {_.Status}");
        var marker = ordered.Length > latest.Count ? $" {TruncatedMarker}" : string.Empty;
        return $"step {step.Id.Value:D} type={Sanitize(step.Type.Value)} status={step.Status} history{marker}=[{string.Join(", ", history)}]";
    }

    static bool IsFailedStep(JobStepStatus status) => status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure;

    static string DescribeObserver(string role, string name, ReactorState state) =>
        Describe(role, $"{Sanitize(name)} id={Sanitize(state.Id.Value)}", new(state.RunningState, state.IsSubscribed, state.NextEventSequenceNumber, state.LastHandledEventSequenceNumber, state.TailEventSequenceNumber));

    static string DescribeObserver(string role, string name, ReducerState state) =>
        Describe(role, Sanitize(name), new(state.RunningState, state.IsSubscribed, state.NextEventSequenceNumber, state.LastHandledEventSequenceNumber, state.TailEventSequenceNumber));

    static string DescribeObserver(string role, string name, ProjectionState state) =>
        Describe(role, Sanitize(name), new(state.RunningState, state.IsSubscribed, state.NextEventSequenceNumber, state.LastHandledEventSequenceNumber, state.TailEventSequenceNumber));

    static string Describe(string role, string identity, ObserverProgress progress) =>
        $"{role} {identity}: state={progress.Running} subscribed={progress.Subscribed} next={progress.Next.Value} lastHandled={progress.LastHandled.Value} tail={progress.Tail.Value}";

    static string Sanitize(string value) =>
        Cap(string.Concat(value.Take(MaxIdentifierLength + 1).Select(_ => IsUnsafe(_) ? '?' : _)), MaxIdentifierLength);

    static bool IsUnsafe(char character) =>
        char.IsControl(character) || char.GetUnicodeCategory(character) is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;

    static string Cap(string value, int length) => value.Length <= length ? value : value[..(length - TruncatedMarker.Length)] + TruncatedMarker;

    readonly record struct ObserverProgress(ObserverRunningState Running, bool Subscribed, EventSequenceNumber Next, EventSequenceNumber LastHandled, EventSequenceNumber Tail);
}
