// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_the_same_key_seeded_twice : Specification
{
    ReadModelScenario<SeededKeyModel> _scenario;
    Exception? _exception;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSourceId("invoice-1").ReadModel(new("invoice-1", "first", "retained", 5));
    }

    void Because() => _exception = Catch.Exception(() => _scenario.CollectReadModelFor("invoice-1", new("invoice-1", "second", "other", 10)));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_refuse_the_duplicate_seed() => _exception.ShouldBeOfExactType<ReadModelAlreadySeeded>();
    [Fact] void should_preserve_the_first_seed() => _scenario.Instance!.Count.ShouldEqual(5);
}
