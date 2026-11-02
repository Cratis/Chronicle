// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

public class and_the_name_is_an_always_true_predicate_on_sqlite : and_the_name_is_an_always_true_predicate<SqlSinkHarness>
{
    [Fact] void should_not_fail_the_read() => _error.ShouldBeNull();
    [Fact] void should_return_no_instances() => InstanceCount().ShouldEqual(0);
    [Fact] void should_not_create_a_table() => _created.ShouldBeFalse();
}
