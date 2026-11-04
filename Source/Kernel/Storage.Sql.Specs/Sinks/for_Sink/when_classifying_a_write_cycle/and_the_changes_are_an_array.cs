// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_classifying_a_write_cycle;

public class and_the_changes_are_an_array : given.a_write_cycle_classification
{
    void Because() => ClassifyAndMeasure(JoinAndDirect());

    [Fact] void should_be_multi_write() => _result.ShouldBeTrue();
    [Fact] void should_not_allocate() => _allocated.ShouldEqual(0);
}
