// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_classifying_a_write_cycle;

public class and_it_has_two_joins : given.a_write_cycle_classification
{
    void Because() => _result = Sink.IsMultiWrite([Join(), Join()]);

    [Fact] void should_be_multi_write() => _result.ShouldBeTrue();
}
