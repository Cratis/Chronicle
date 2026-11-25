// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_MachineLoad;

public class when_parsing_garbage : Specification
{
    LoadReading _reading = LoadReading.Unknown;
    bool _quiet = true;

    void Because()
    {
        _reading = MachineLoad.Parse("not a load");
        _quiet = MachineLoad.IsQuietEnough(_reading, 12);
    }

    [Fact] void should_be_unknown() => double.IsNaN(_reading.OneMinute).ShouldBeTrue();
    [Fact] void should_never_be_assumed_quiet() => _quiet.ShouldBeFalse();
}
