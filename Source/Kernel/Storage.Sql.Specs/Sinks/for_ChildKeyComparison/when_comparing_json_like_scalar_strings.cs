// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison;

public class when_comparing_json_like_scalar_strings : Specification
{
    readonly string[] _keys = ["{broken", "{ad037c15-1354-4cd7-a069-f24aa1706109}", "[x]"];
    bool[] _matches;

    void Because() => _matches = _keys.Select(key => ChildKeyComparison.AreEqual(key, key)).ToArray();

    [Fact] void should_match_malformed_json_text_to_itself() => _matches[0].ShouldBeTrue();
    [Fact] void should_match_a_braced_guid_to_itself() => _matches[1].ShouldBeTrue();
    [Fact] void should_match_bracketed_text_to_itself() => _matches[2].ShouldBeTrue();
}
