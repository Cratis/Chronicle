// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
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
    /// Runs the wait once; when it is canceled or times out, throws <see cref="ReplayWaitDidNotComplete"/> with a bounded snapshot.
    /// </summary>
    /// <param name="wait">The original wait.</param>
    /// <param name="sources">Where to read the snapshot from.</param>
    /// <param name="createBudget">Optional factory of the single shared budget for the snapshot reads; defaults to five seconds.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ReplayWaitDidNotComplete">The wait was canceled or timed out; the original exception is the inner exception.</exception>
    public static async Task Run(Func<Task> wait, ReplayWaitSources sources, Func<CancellationTokenSource>? createBudget = null)
    {
        try
        {
            await wait();
        }
        catch (Exception original) when (original is OperationCanceledException or TimeoutException)
        {
            throw new ReplayWaitDidNotComplete(await TakeSnapshot(sources, createBudget ?? DefaultBudget), original);
        }
    }

    static CancellationTokenSource DefaultBudget() => new(TimeSpan.FromSeconds(5));

    static async Task<string> TakeSnapshot(ReplayWaitSources sources, Func<CancellationTokenSource> createBudget)
    {
        try
        {
            using var budget = createBudget();
            var token = budget.Token;
            var sections = await Task.WhenAll(
                Read(async () => DescribeJobs(await sources.GetJobs()), token),
                Read(async () => DescribeObserver("reactor", sources.ReactorName, await sources.GetReactorState()), token),
                Read(async () => DescribeObserver("reducer", sources.ReducerName, await sources.GetReducerState()), token),
                Read(async () => DescribeObserver("projection", sources.ProjectionName, await sources.GetProjectionState()), token));
            return Cap(string.Join(Environment.NewLine, sections.Select(_ => Cap(_, MaxSectionLength))), MaxSummaryLength);
        }
        catch (Exception exception)
        {
            return $"snapshot failed: {exception.GetType().Name}";
        }
    }

    static async Task<string> Read(Func<Task<string>> reader, CancellationToken budget)
    {
        try
        {
            var read = reader();
            _ = read.ContinueWith(static task => task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return await read.WaitAsync(budget);
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested)
        {
            return "snapshot timed out";
        }
        catch (Exception exception)
        {
            return $"snapshot failed: {exception.GetType().Name}";
        }
    }

    static string DescribeJobs(IEnumerable<Job> jobs)
    {
        var shown = jobs.Take(MaxJobs + 1).ToArray();
        if (shown.Length == 0)
        {
            return "jobs: none outstanding";
        }

        var builder = new StringBuilder("jobs:");
        foreach (var job in shown.Take(MaxJobs))
        {
            var progress = job.Progress;
            builder.Append(Environment.NewLine)
                .Append($"  job {job.Id.Value:D} type={Sanitize(job.Type.Value)} status={job.Status} steps total={progress.TotalSteps} successful={progress.SuccessfulSteps} failed={progress.FailedSteps} stopped={progress.StoppedSteps} isCompleted={progress.IsCompleted} isStopped={progress.IsStopped}");
        }

        if (shown.Length > MaxJobs)
        {
            builder.Append(Environment.NewLine).Append($"  more jobs omitted {TruncatedMarker}");
        }

        return builder.ToString();
    }

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
