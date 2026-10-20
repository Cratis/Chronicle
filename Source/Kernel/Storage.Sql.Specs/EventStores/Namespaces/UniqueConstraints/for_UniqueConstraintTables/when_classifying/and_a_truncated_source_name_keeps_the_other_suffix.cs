// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_a_truncated_source_name_keeps_the_other_suffix : Specification
{
    string _name;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish() => _name = $"event-log_{new string('a', 35)}_constraint_values";

    void Because() => _result = UniqueConstraintTables.Classify(
        [_name], EventSequenceId.Log, [$"{_name}|EventSourceId"]);

    [Fact] void should_classify_by_the_primary_key() => _result.PerEventSource.ShouldContainOnly(_name);
    [Fact] void should_not_classify_by_the_misleading_suffix() => _result.PerValue.ShouldBeEmpty();
}
