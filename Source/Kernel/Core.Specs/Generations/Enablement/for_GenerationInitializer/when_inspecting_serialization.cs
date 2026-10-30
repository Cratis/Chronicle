// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_inspecting_serialization : Specification
{
    [Fact] void should_be_an_orleans_grain() => typeof(Grain).IsAssignableFrom(typeof(GenerationInitializer)).ShouldBeTrue();
    [Fact] void should_not_be_reentrant() => Attribute.IsDefined(typeof(GenerationInitializer), typeof(ReentrantAttribute)).ShouldBeFalse();
    [Fact] void should_have_a_string_grain_key() => typeof(IGrainWithStringKey).IsAssignableFrom(typeof(IGenerationInitializer)).ShouldBeTrue();
}
