// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

public class an_event_log_with_append_diagnostics(ChronicleFixture fixture) : Specification(fixture)
{
    protected override Task OnBeforeInitializeAsync() => LogInitialization(base.OnBeforeInitializeAsync);

    protected override Task OnInitializeAsync() => LogInitialization(base.OnInitializeAsync);

    protected async Task<TResult> AppendWithDiagnostics<TResult>(Func<Task<TResult>> append, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        where TResult : IAppendResult
    {
        // DIAGNOSTIC (#5778 dry run) — remove
        var location = $"{GetType().FullName}.{member}:{line}";
        await LogPendingJobs(location);
        try
        {
            var result = await append();
            Console.WriteLine($"DIAGNOSTIC (#5778) {DateTimeOffset.UtcNow:O} {location} {result.GetType().Name}: IsSuccess={result.IsSuccess}; ConstraintViolations={JsonSerializer.Serialize(result.ConstraintViolations)}; Errors={JsonSerializer.Serialize(result.Errors)}");
            return result;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"DIAGNOSTIC (#5778) {DateTimeOffset.UtcNow:O} {location} append exception: {exception}");
            throw;
        }
    }

    async Task LogPendingJobs(string location)
    {
        try
        {
            var jobs = (await EventStore.Jobs.GetJobs()).Where(job => job.Status is not JobStatus.CompletedSuccessfully and not JobStatus.CompletedWithFailures).ToArray();
            Console.WriteLine($"DIAGNOSTIC (#5778) {DateTimeOffset.UtcNow:O} {location} before append: non-completed jobs={jobs.Length}; {string.Join("; ", jobs.Select(job => $"Id={job.Id}; Type={job.Type}; Status={job.Status}"))}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"DIAGNOSTIC (#5778) {DateTimeOffset.UtcNow:O} {location} jobs snapshot exception: {exception}");
        }
    }

    async Task LogInitialization(Func<Task> initialize)
    {
        try
        {
            await initialize();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"DIAGNOSTIC (#5778) {DateTimeOffset.UtcNow:O} {GetType().FullName} InitializeAsync exception: {exception}");
            throw;
        }
    }
}
