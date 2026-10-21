// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_truncated_names_share_a_prefix : Specification
{
    string _source;
    string _value;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        var prefix = $"event-log_{new string('a', 40)}";
        _source = $"{prefix}_source_{new string('b', 30)}_constraint"[..63];
        _value = $"{prefix}_value_{new string('b', 30)}_constraint_values"[..63];
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_source, _value], EventSequenceId.Log, [$"{_source}|EventSourceId", $"{_value}|Value"]);

    [Fact] void should_only_find_the_source_table_by_its_own_key() => _result.PerEventSource.ShouldContainOnly(_source);
    [Fact] void should_only_find_the_value_table_by_its_own_key() => _result.PerValue.ShouldContainOnly(_value);
}
