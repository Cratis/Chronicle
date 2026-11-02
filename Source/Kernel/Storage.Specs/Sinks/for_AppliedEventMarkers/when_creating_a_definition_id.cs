// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_AppliedEventMarkers;

public class when_creating_a_definition_id : Specification
{
    string _id;
    string _same;
    string _otherType;
    string _otherPath;

    void Because()
    {
        _id = AppliedEventMarkers.DefinitionId("type-a", "customer.name");
        _same = AppliedEventMarkers.DefinitionId("type-a", "customer.name");
        _otherType = AppliedEventMarkers.DefinitionId("type-b", "customer.name");
        _otherPath = AppliedEventMarkers.DefinitionId("type-a", "customer.other");
    }

    [Fact] void should_be_stable() => _id.ShouldEqual(_same);
    [Fact] void should_be_11_url_safe_characters() => System.Text.RegularExpressions.Regex.IsMatch(_id, "^[A-Za-z0-9_-]{11}$").ShouldBeTrue();
    [Fact] void should_differ_per_event_type() => _id.ShouldNotEqual(_otherType);
    [Fact] void should_differ_per_path() => _id.ShouldNotEqual(_otherPath);
}
