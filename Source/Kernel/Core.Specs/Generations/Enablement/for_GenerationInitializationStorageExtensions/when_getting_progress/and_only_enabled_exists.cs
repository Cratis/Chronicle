// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializationStorageExtensions.when_getting_progress;

public class and_only_enabled_exists : Specification
{
    IGenerationInitializationStorage _storage;
    GenerationInitializationProgress _result;

    void Establish()
    {
        _storage = Substitute.For<IGenerationInitializationStorage>();
        _storage.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.Enabled, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
    }

    async Task Because() => _result = await _storage.GetProgress();

    [Fact] void should_report_adopted() => _result.ShouldEqual(GenerationInitializationProgress.Adopted);
}
