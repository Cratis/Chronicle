// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintTables.when_classifying;

public class and_a_multibyte_name_reaches_the_byte_limit : Specification
{
    string _name;
    (string[] PerEventSource, string[] PerValue) _result;

    void Establish() => _name = $"event-log_{new string('é', 25)}abc";

    void Because() => _result = UniqueConstraintTables.Classify(
        [_name], EventSequenceId.Log, [$"{_name}|EventSourceId"]);

    [Fact] void should_measure_bytes_instead_of_characters() => _result.PerEventSource.ShouldContainOnly(_name);
    [Fact] void should_not_find_a_value_table() => _result.PerValue.ShouldBeEmpty();
}
