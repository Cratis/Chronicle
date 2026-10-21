// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_logical_names_collide_after_truncation : Specification
{
    string[] _names;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish()
    {
        var prefix = $"event-log_{new string('a', 80)}";
        _names = new[] { $"{prefix}_constraint", $"{prefix}_constraint_values" }
            .Select(name => name[..63]).Distinct().ToArray();
    }

    void Because() => _result = UniqueConstraintTables.Classify(
        _names, EventSequenceId.Log, [$"{_names[0]}|Value"]);

    [Fact] void should_classify_the_single_physical_table_by_its_key() => _result.PerValue.ShouldContainOnly(_names);
    [Fact] void should_not_also_classify_it_as_per_event_source() => _result.PerEventSource.ShouldBeEmpty();
}
