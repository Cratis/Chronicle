// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializationStorageExtensions.when_getting_progress;

public class and_reading_receipts_fails : Specification
{
    IGenerationInitializationStorage _storage;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IGenerationInitializationStorage>();
        _storage.GetReceipts().Returns<Task<IReadOnlyList<GenerationInitializationReceipt>>>(_ => throw new InvalidOperationException("unavailable"));
    }

    async Task Because() => _error = await Catch.Exception(_storage.GetProgress);

    [Fact] void should_propagate_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
