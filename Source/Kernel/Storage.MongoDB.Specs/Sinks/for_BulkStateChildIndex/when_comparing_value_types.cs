// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_comparing_value_types : given.an_index
{
    readonly Guid _id = new("b3674fc4-81a8-434c-8b61-64254873ef99");
    string[] _guidMatches;
    string[] _stringMatches;
    string[] _numberMatches;
    string[] _conceptMatches;
    string[] _decimalMatches;

    void Establish()
    {
        Set("guid", Children(_id));
        Set("string", Children("text"));
        Set("number", Children(42));
        Set("decimal", Children(1.0m));
        Set("concept", Children(new EventSourceId("concept")));
    }

    void Because()
    {
        _guidMatches = Find("[children].id", _id.ToString());
        _stringMatches = Find("[children].id", "text");
        _numberMatches = Find("[children].id", 42L);
        _decimalMatches = Find("[children].id", 1m);
        _conceptMatches = Find("[children].id", "concept");
    }

    [Fact] void should_match_a_guid_against_its_string() => _guidMatches.ShouldContainOnly("guid");
    [Fact] void should_match_equal_decimals_with_different_string_representations() => _decimalMatches.ShouldContainOnly("decimal");
    [Fact] void should_match_a_string() => _stringMatches.ShouldContainOnly("string");
    [Fact] void should_match_an_int_against_a_long() => _numberMatches.ShouldContainOnly("number");
    [Fact] void should_match_a_concept_against_its_string() => _conceptMatches.ShouldContainOnly("concept");
}
