// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_checking_isolated_replace_support : given.a_partition_rebuild_sink
{
    [Fact] void should_not_offer_isolated_replace() => _sink.SupportsIsolatedReplace.ShouldBeFalse();
}
