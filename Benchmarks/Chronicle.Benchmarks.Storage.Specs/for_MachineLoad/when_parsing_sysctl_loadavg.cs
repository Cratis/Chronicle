// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_MachineLoad;

public class when_parsing_sysctl_loadavg : Specification
{
    LoadReading _reading = LoadReading.Unknown;

    void Because() => _reading = MachineLoad.Parse("{ 1.50 2.50 3.50 }");

    [Fact] void should_read_the_one_minute_load() => _reading.OneMinute.ShouldEqual(1.5);
    [Fact] void should_read_the_fifteen_minute_load() => _reading.FifteenMinute.ShouldEqual(3.5);
}
