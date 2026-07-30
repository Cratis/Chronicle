// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureChangeDetector.when_detecting;

public class and_nothing_has_changed : Specification
{
    CaptureChangeDetector _detector;
    IEnumerable<CaptureChange> _result;

    void Establish() => _detector = new();

    void Because() => _result = _detector.Detect(
        new Dictionary<string, JsonObject> { ["42"] = new() { ["name"] = "First" } },
        new Dictionary<string, JsonObject> { ["42"] = new() { ["name"] = "First" } });

    [Fact] void should_have_no_changes() => _result.ShouldBeEmpty();
}
