// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Jobs.for_JobsWaitHelpers;

public class when_timing_a_successful_job_poll : Specification
{
    readonly List<string> _lines = [];
    readonly Job[] _expected = [];
    IEnumerable<Job> _result;
    JsonElement _record;
    IJobs _jobs;

    void Establish()
    {
        _jobs = Substitute.For<IJobs>();
        _jobs.GetJobs().Returns(_expected);
    }

    async Task Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 0, 1000, "spec");
        using (var scenario = timings.Begin("scenario"))
        {
            _result = await _jobs.WaitForThereToBeNoJobs();
            scenario?.Success();
        }
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[2][(_lines[2].IndexOf(' ') + 1)..]);
    }

    [Fact] void should_preserve_the_result() => _result.ShouldEqual(_expected);
    [Fact] void should_record_one_poll() => _record.GetProperty("polls").GetInt32().ShouldEqual(1);
    [Fact] void should_record_success() => _record.GetProperty("outcome").GetString().ShouldEqual("success");
    [Fact] void should_not_add_a_query() => _jobs.Received(1).GetJobs();
}
