// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_MachineLoad;

public class when_parsing_proc_loadavg : Specification
{
    LoadReading _reading = LoadReading.Unknown;

    void Because() => _reading = MachineLoad.Parse("0.50 0.40 0.30 1/200 1234");

    [Fact] void should_read_the_one_minute_load() => _reading.OneMinute.ShouldEqual(0.5);
    [Fact] void should_read_the_five_minute_load() => _reading.FiveMinute.ShouldEqual(0.4);
    [Fact] void should_read_the_fifteen_minute_load() => _reading.FifteenMinute.ShouldEqual(0.3);
}
