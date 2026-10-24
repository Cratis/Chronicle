// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineSettings;

public class when_mapping_an_unknown_topology : Specification
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => RebuildBaselineSettings.ParseTopology("unknown"));
    [Fact] void should_reject_the_topology() => _error.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_list_the_supported_topologies() => _error!.Message.ShouldEqual("REBUILD_TOPOLOGY must be single, two, split-jobs or three.");
}
