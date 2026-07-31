// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureChangeDetector.when_detecting;

public class and_an_item_is_gone : Specification
{
    CaptureChangeDetector _detector;
    IEnumerable<CaptureChange> _result;

    void Establish() => _detector = new();

    void Because() => _result = _detector.Detect(
        new Dictionary<string, JsonObject> { ["42"] = new() { ["name"] = "First" } },
        new Dictionary<string, JsonObject>());

    [Fact] void should_have_one_change() => _result.Count().ShouldEqual(1);
    [Fact] void should_be_a_removed_change() => _result.First().Type.ShouldEqual(CaptureChangeType.Removed);
    [Fact] void should_have_the_key() => _result.First().Key.ShouldEqual("42");
    [Fact] void should_have_the_previous_item() => _result.First().Previous.ShouldNotBeNull();
    [Fact] void should_not_have_a_current_item() => _result.First().Current.ShouldBeNull();
}
