// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

public class and_composite_keys_fail_for_different_reasons : given.a_sink_with_unreadable_composite_keys
{
    ExpandoObject[] _firstRead;
    ExpandoObject[] _secondRead;

    async Task Because()
    {
        _firstRead = (await _sink.GetInstances()).Instances.ToArray();
        _secondRead = (await _sink.GetInstances()).Instances.ToArray();
    }

    [Fact] void should_return_all_instances_on_the_first_read() => _firstRead.Length.ShouldEqual(4);
    [Fact] void should_return_all_instances_on_the_second_read() => _secondRead.Length.ShouldEqual(4);
    [Fact] void should_warn_once_for_the_separator_reason() => _logger.Warnings.Count(warning => warning.Contains(SeparatorReason)).ShouldEqual(1);
    [Fact] void should_warn_once_for_the_empty_part_reason() => _logger.Warnings.Count(warning => warning.Contains(EmptyPartReason)).ShouldEqual(1);
    [Fact] void should_only_warn_for_the_two_reasons() => _logger.Warnings.Count().ShouldEqual(2);
}
