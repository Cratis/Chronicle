// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_timing_an_unavailable_tail : Specification
{
    readonly List<string> _lines = [];
    AppendResultWaitForCompletionResult _result;
    JsonElement _record;

    async Task Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 0, 1000, "spec");
        using (var scenario = timings.Begin("scenario"))
        {
            var append = new AppendResult { SequenceNumber = Events.EventSequenceNumber.Unavailable };
            _result = await append.WaitForCompletion();
            scenario?.Success();
        }
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[2][(_lines[2].IndexOf(' ') + 1)..]);
    }

    [Fact] void should_preserve_trivial_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_no_rpc() => _record.GetProperty("polls").GetInt32().ShouldEqual(0);
    [Fact] void should_record_success() => _record.GetProperty("outcome").GetString().ShouldEqual("success");
}
