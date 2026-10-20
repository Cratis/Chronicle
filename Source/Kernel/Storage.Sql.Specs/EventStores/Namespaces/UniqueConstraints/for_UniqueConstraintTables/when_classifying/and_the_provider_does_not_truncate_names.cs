// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_the_provider_does_not_truncate_names : Specification
{
    string _source;
    string _value;
    string _withoutSuffix;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        _source = $"event-log_{new string('a', 80)}_constraint";
        _value = $"event-log_{new string('b', 80)}_constraint_values";
        _withoutSuffix = $"event-log_{new string('c', 80)}";
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_source, _value, _withoutSuffix, "other_name_constraint"], EventSequenceId.Log, null);

    [Fact] void should_still_classify_source_tables_by_suffix() => _result.PerEventSource.ShouldContainOnly(_source);
    [Fact] void should_still_classify_value_tables_by_suffix() => _result.PerValue.ShouldContainOnly(_value);
}
