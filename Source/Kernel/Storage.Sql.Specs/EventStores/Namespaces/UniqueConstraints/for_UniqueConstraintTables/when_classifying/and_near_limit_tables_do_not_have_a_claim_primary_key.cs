// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_near_limit_tables_do_not_have_a_claim_primary_key : Specification
{
    string _composite;
    string _unrelated;
    string _missing;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        _composite = $"event-log_{new string('a', 42)}_constraint";
        _unrelated = $"event-log_{new string('b', 35)}_constraint_values";
        _missing = $"event-log_{new string('c', 53)}";
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_composite, _unrelated, _missing],
        EventSequenceId.Log,
        [$"{_composite}|EventSourceId", $"{_composite}|Value", $"{_unrelated}|Id"]);

    [Fact] void should_not_classify_any_source_tables() => _result.PerEventSource.ShouldBeEmpty();
    [Fact] void should_not_classify_any_value_tables() => _result.PerValue.ShouldBeEmpty();
}
