// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_crashing_after_every_write : given.an_initializer
{
    int _attempts;

    async Task Because()
    {
        await _initializer.Initialize(_name);
        var count = _writes;
        var expected = (await _audit.GetReceipts()).Select(_ => (_.Step, _.Subject, _.Evidence)).ToArray();
        for (var write = 1; write <= count; write++)
        {
            Reset();
            _failAfter = write;
            var error = await Catch.Exception(() => _initializer.Initialize(_name));
            error.ShouldBeOfExactType<simulated_crash>();
            _result = await Create().Initialize(_name);
            (_result is GenerationInitializationEnabled or GenerationInitializationAlreadyEnabled).ShouldBeTrue();
            (await _audit.GetReceipts()).Select(_ => (_.Step, _.Subject, _.Evidence)).ShouldEqual(expected);
            _catalog.Records.Count.ShouldEqual(2);
            _catalog.Epochs.Count.ShouldEqual(2);
            (await _audit.GetBlockers()).Any(_ => _.ResolvedAt is null).ShouldBeFalse();
            _attempts++;
        }
    }
    [Fact] void should_exercise_every_write() => (_attempts > 10).ShouldBeTrue();
}
