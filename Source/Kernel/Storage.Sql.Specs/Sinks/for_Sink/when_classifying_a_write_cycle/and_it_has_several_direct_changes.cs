// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_classifying_a_write_cycle;

public class and_it_has_several_direct_changes : given.a_write_cycle_classification
{
    void Because() => _result = Sink.IsMultiWrite([Direct(), Direct()]);

    [Fact] void should_not_be_multi_write_because_direct_changes_are_one_write() => _result.ShouldBeFalse();
}
