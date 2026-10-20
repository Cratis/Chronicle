// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_names_fit_the_byte_limit_exactly : Specification
{
    string _source;
    string _value;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        _source = $"event-log_{new string('a', 42)}_constraint";
        _value = $"event-log_{new string('b', 35)}_constraint_values";
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_source, _value], EventSequenceId.Log, [$"{_source}|EventSourceId", $"{_value}|Value"]);

    [Fact] void should_find_the_complete_source_name() => _result.PerEventSource.ShouldContainOnly(_source);
    [Fact] void should_find_the_complete_value_name() => _result.PerValue.ShouldContainOnly(_value);
}
