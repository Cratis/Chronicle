// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_the_action_fails : Specification
{
    readonly List<string> _lines = [];
    readonly Exception _original = new InvalidOperationException("Original failure");
    Exception? _caught;
    JsonElement _phase;

    async Task Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 10, 1000, "fixture");
        _caught = await Catch.Exception(() => timings.Run("scenario", () => Task.FromException(_original)));
        var line = _lines[1];
        _phase = JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]);
    }

    [Fact] void should_preserve_the_original_exception() => _caught.ShouldBeSame(_original);
    [Fact] void should_record_the_failed_phase() => _phase.GetProperty("outcome").GetString().ShouldEqual("failure");
    [Fact] void should_close_the_ambient_scope() => IntegrationTimings.CurrentPhase.ShouldBeNull();
}
