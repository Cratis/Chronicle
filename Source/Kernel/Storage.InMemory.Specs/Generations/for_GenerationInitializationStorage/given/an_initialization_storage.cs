// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationInitializationStorage.given;

public class an_initialization_storage : Specification
{
    protected IGenerationInitializationStorage _audit;

    protected GenerationInitializationReceipt Receipt(string evidence = "original") => new(new("orders"), GenerationInitializationStep.FloorVerified, "store", evidence, new(Guid.Parse("93091777-2010-4eba-802b-2dcc773c2d06")), 1, DateTimeOffset.FromUnixTimeSeconds(30000));
    protected GenerationInitializationBlocker Blocker(DateTimeOffset at) => new(new("orders"), GenerationInitializationBlockerKind.ExclusionNotProven, "store", "not proven", at, null);

    void Establish() => _audit = new Generations.GenerationInitializationStorage();
}
