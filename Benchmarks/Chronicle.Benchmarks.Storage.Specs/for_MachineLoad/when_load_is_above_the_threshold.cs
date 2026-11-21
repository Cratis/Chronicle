// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_MachineLoad;

public class when_load_is_above_the_threshold : Specification
{
    bool _quiet = true;

    void Because() => _quiet = MachineLoad.IsQuietEnough(new(13, 23, 25), 12);

    [Fact] void should_refuse_to_measure() => _quiet.ShouldBeFalse();
    [Fact] void should_use_seventy_percent_of_the_cpus_by_default() => MachineLoad.Threshold(10).ShouldEqual(7.0);
}
