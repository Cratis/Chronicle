// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_postgresql_truncated_the_names : Specification
{
    const string Sequence = "event-log";
    string _perEventSource;
    string _perValue;
    string _unrelated;
    string _short;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        var longName = new string('a', 80);
        _perEventSource = $"{Sequence}_source_{longName}_constraint"[..63];
        _perValue = $"{Sequence}_value_{longName}_constraint_values"[..63];
        _unrelated = $"{Sequence}_other_{longName}_readmodel"[..63];
        _short = $"{Sequence}_name_constraint";
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_perEventSource, _perValue, _unrelated, _short, "other_x_constraint"],
        new EventSequenceId(Sequence),
        [$"{_perEventSource}|EventSourceId", $"{_perValue}|Value", $"{_unrelated}|Id"]);

    [Fact] void should_find_the_truncated_per_event_source_table() => _result.PerEventSource.ShouldContain(_perEventSource);
    [Fact] void should_find_the_truncated_per_value_table() => _result.PerValue.ShouldContain(_perValue);
    [Fact] void should_still_find_the_suffixed_table() => _result.PerEventSource.ShouldContain(_short);
    [Fact] void should_not_find_an_unrelated_table() => _result.PerEventSource.Concat(_result.PerValue).ShouldNotContain(_unrelated);
    [Fact] void should_not_find_another_sequences_table() => _result.PerEventSource.ShouldNotContain("other_x_constraint");
    [Fact] void should_find_exactly_two_per_event_source_tables() => _result.PerEventSource.Length.ShouldEqual(2);
}
