// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

public class and_a_second_sink_reads_unreadable_composite_keys : given.a_sink_with_unreadable_composite_keys
{
    string[] _firstSinkWarnings;
    string[] _secondSinkWarnings;
    ExpandoObject[] _instances;

    async Task Because()
    {
        _ = (await _sink.GetInstances()).Instances.ToArray();
        _ = (await _sink.GetInstances()).Instances.ToArray();
        _firstSinkWarnings = _logger.Warnings.ToArray();
        var secondSink = _harness.CreateSinkForTheSameReadModel();
        _instances = (await secondSink.GetInstances()).Instances.ToArray();
        _ = (await secondSink.GetInstances()).Instances.ToArray();
        _secondSinkWarnings = _logger.Warnings.Skip(_firstSinkWarnings.Length).ToArray();
    }

    [Fact] void should_return_all_instances_from_the_second_sink() => _instances.Length.ShouldEqual(4);
    [Fact] void should_warn_once_per_reason_for_the_first_sink() => _firstSinkWarnings.Length.ShouldEqual(2);
    [Fact] void should_warn_again_once_for_the_separator_reason() => _secondSinkWarnings.Count(warning => warning.Contains(SeparatorReason)).ShouldEqual(1);
    [Fact] void should_warn_again_once_for_the_empty_part_reason() => _secondSinkWarnings.Count(warning => warning.Contains(EmptyPartReason)).ShouldEqual(1);
    [Fact] void should_only_warn_once_per_reason_for_the_second_sink() => _secondSinkWarnings.Length.ShouldEqual(2);
}
