// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Jobs.for_JobsWaitHelpers;

public class when_timing_a_failed_job_poll : Specification
{
    readonly List<string> _lines = [];
    readonly Exception _original = new InvalidOperationException("poll failed");
    Exception? _caught;
    JsonElement _record;
    IJobs _jobs;

    void Establish()
    {
        _jobs = Substitute.For<IJobs>();
        _jobs.GetJobs().Returns(Task.FromException<IEnumerable<Job>>(_original));
    }

    async Task Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 0, 1000, "spec");
        using (var scenario = timings.Begin("scenario"))
        {
            try { await _jobs.WaitForThereToBeJobs(); }
            catch (Exception exception) { _caught = exception; }
            scenario?.Success();
        }
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[2][(_lines[2].IndexOf(' ') + 1)..]);
    }

    [Fact] void should_preserve_the_original_exception() => _caught.ShouldEqual(_original);
    [Fact] void should_record_one_poll() => _record.GetProperty("polls").GetInt32().ShouldEqual(1);
    [Fact] void should_record_failure() => _record.GetProperty("outcome").GetString().ShouldEqual("failure");
    [Fact] void should_not_retry_the_failed_call() => _jobs.Received(1).GetJobs();
}
