// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_a_value_contains_the_old_separator : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _leftDash;
    string _rightDash;
    string _leftPrefix;
    string _rightPrefix;

    void Because()
    {
        _leftDash = Value("a-b", "c");
        _rightDash = Value("a", "b-c");
        _leftPrefix = Value("P", "SEP-X");
        _rightPrefix = Value("P-SEP", "X");
    }

    string Value(string first, string second) => UniqueConstraintEncoder.Encode(_key, [new(first), new(second)]).AsT0.Value;

    [Fact] void should_not_alias_a_dash_moved_between_components() => _leftDash.ShouldNotEqual(_rightDash);
    [Fact] void should_not_alias_a_separator_inside_the_second_component() => _leftPrefix.ShouldNotEqual(_rightPrefix);
    [Fact] void should_not_alias_across_the_two_pairs() => _leftDash.ShouldNotEqual(_leftPrefix);
}
