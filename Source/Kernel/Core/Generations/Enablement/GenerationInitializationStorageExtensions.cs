// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Extension methods for reading initialization progress from durable receipts.
/// </summary>
internal static class GenerationInitializationStorageExtensions
{
    /// <summary>
    /// Classifies progress from the durable receipts. This only reads and never consults the operational flag.
    /// </summary>
    /// <param name="storage">The generation initialization storage.</param>
    /// <returns>The progress; a failure to read propagates so callers fail closed.</returns>
    internal static async Task<GenerationInitializationProgress> GetProgress(this IGenerationInitializationStorage storage)
    {
        var receipts = await storage.GetReceipts();
        if (receipts.Count == 0)
        {
            return GenerationInitializationProgress.NotStarted;
        }

        return receipts.Any(_ => _.Step >= GenerationInitializationStep.MaterializationAdopted)
            ? GenerationInitializationProgress.Adopted
            : GenerationInitializationProgress.Quiescing;
    }
}
