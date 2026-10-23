// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>Reads floors before registration, patches or job recovery; never advances them.</summary>
/// <param name="storage">The cluster storage.</param>
public sealed class GenerationFloorAdmission(IStorage storage) : IGenerationFloorAdmission
{
    /// <inheritdoc/>
    public async Task Admit(CancellationToken cancellationToken)
    {
        var stores = await storage.GetEventStores().WaitAsync(cancellationToken);
        foreach (var store in stores.Append(EventStoreName.System).Distinct())
        {
            await storage.GetEventStore(store).EnsureAdmitted(GenerationBinarySupport.Current).WaitAsync(cancellationToken);
        }
    }
}
