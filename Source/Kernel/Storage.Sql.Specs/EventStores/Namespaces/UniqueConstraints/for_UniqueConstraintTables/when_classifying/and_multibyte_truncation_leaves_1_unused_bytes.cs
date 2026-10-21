// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_multibyte_truncation_leaves_1_unused_bytes : Specification
{
    string _name;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        var prefix = $"event-log_{new string('a', 52)}";
        var logicalName = $"{prefix}é_constraint_values";
        _name = logicalName[..prefix.Length];
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        [_name], EventSequenceId.Log, [$"{_name}|Value"]);

    [Fact] void should_find_the_truncated_value_table() => _result.PerValue.ShouldContainOnly(_name);
    [Fact] void should_not_find_a_source_table() => _result.PerEventSource.ShouldBeEmpty();
}
