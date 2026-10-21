// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_a_truncated_name_contains_a_delimiter : Specification
{
    string _name;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish() => _name = $"event-log_name|{new string('a', 60)}"[..63];

    void Because() => _result = UniqueConstraintTables.Classify(
        [_name], EventSequenceId.Log, [$"{_name}|Value"]);

    [Fact] void should_match_the_entire_table_name() => _result.PerValue.ShouldContainOnly(_name);
    [Fact] void should_not_find_a_source_table() => _result.PerEventSource.ShouldBeEmpty();
}
