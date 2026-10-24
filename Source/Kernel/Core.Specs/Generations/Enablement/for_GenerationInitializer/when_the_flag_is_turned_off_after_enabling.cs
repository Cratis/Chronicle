// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_the_flag_is_turned_off_after_enabling : given.an_initializer
{
    int _firstWrites;

    async Task Establish()
    {
        await _initializer.Initialize(_name);
        _firstWrites = _writes;
        _flag.Enabled.Returns(false);
    }
    async Task Because() => _result = await Create().Initialize(_name);
    [Fact] void should_report_disabled() => _result.ShouldBeOfExactType<GenerationInitializationDisabled>();
    [Fact] void should_write_nothing() => _writes.ShouldEqual(_firstWrites);
    [Fact] async Task should_keep_the_floor() => (await _store.GenerationFloor.Get()).Revision.ShouldEqual(1UL);
}
