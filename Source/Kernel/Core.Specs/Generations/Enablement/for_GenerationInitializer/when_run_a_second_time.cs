// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_run_a_second_time : given.an_initializer
{
    int _firstWrites;

    async Task Establish()
    {
        await _initializer.Initialize(_name);
        _firstWrites = _writes;
    }
    async Task Because() => _result = await Create().Initialize(_name);
    [Fact] void should_report_already_enabled() => _result.ShouldBeOfExactType<GenerationInitializationAlreadyEnabled>();
    [Fact] void should_write_nothing() => _writes.ShouldEqual(_firstWrites);
}
