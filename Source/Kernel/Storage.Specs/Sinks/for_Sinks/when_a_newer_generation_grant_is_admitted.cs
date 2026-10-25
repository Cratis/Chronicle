// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_a_newer_generation_grant_is_admitted : Specification
{
    GenerationSinkContext _older;
    GenerationSinkContext _newer;
    GenerationSinkContext _otherGeneration;
    ISinkFactory _factory;
    Sinks _sinks;
    ISink _first;
    ISink _afterNewer;
    ISink _newerAgain;
    ISink _olderAgain;
    ISink _otherBefore;
    ISink _otherAfter;

    void Establish()
    {
        _older = GenerationContexts.Create(WellKnownSinkTypes.InMemory);
        _newer = _older with { Grant = _older.Grant with { Epoch = 2 } };
        _otherGeneration = GenerationContexts.Create(WellKnownSinkTypes.InMemory, 3);
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(WellKnownSinkTypes.InMemory);
        _factory.CreateFor("store", "namespace", Arg.Any<ReadModelDefinition>()).Returns(_ => Substitute.For<ISink>());
        _factory.CreateFor("store", "namespace", Arg.Any<GenerationSinkContext>()).Returns(_ => Substitute.For<ISink>());
        _sinks = new("store", "namespace", new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    async Task Because()
    {
        _first = await _sinks.GetFor(_older);
        _otherBefore = await _sinks.GetFor(_otherGeneration);
        _afterNewer = await _sinks.GetFor(_newer);
        _newerAgain = await _sinks.GetFor(_newer);
        _olderAgain = await _sinks.GetFor(_older);
        _otherAfter = await _sinks.GetFor(_otherGeneration);
    }

    [Fact] void should_create_a_sink_for_the_newer_grant() => _afterNewer.ShouldNotBeSame(_first);
    [Fact] void should_keep_serving_the_newer_grant_from_the_cache() => _newerAgain.ShouldBeSame(_afterNewer);
    [Fact] void should_have_dropped_the_superseded_cache_entry() => _olderAgain.ShouldNotBeSame(_first);
    [Fact] void should_not_touch_another_generation() => _otherAfter.ShouldBeSame(_otherBefore);
}
