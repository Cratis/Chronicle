// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_finding_before_any_write : given.a_partition_rebuild_sink
{
    ExpandoObject? _result;

    async Task Because() => _result = await _sink.FindOrDefault(_partition);

    [Fact] void should_find_nothing() => _result.ShouldBeNull();
    [Fact] void should_not_be_removed() => _sink.IsRemoved.ShouldBeFalse();
}
