// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Decides whether a destructive operation may touch a collection, given the collections a read model's sink owns and the
/// lifecycle phase it is in.
/// </summary>
/// <remarks>
/// A destructive operation may only ever touch the collection its caller owns in its current phase. The guards fail loudly
/// with <see cref="SinkCollectionOperationRefused"/> instead of letting a mistaken or raced operation remove the data
/// readers depend on.
/// </remarks>
public static class SinkCollectionGuards
{
    /// <summary>Refuses operations that could destroy a claimed generation.</summary>
    /// <param name="operation">The collection operation.</param>
    /// <param name="collection">The physical collection being mutated.</param>
    /// <param name="ownPrimary">The sink's own physical container.</param>
    /// <param name="claims">Every generation claiming this container.</param>
    /// <exception cref="SinkCollectionOperationRefused">A generation protects the container.</exception>
    public static void ThrowIfGenerationProtected(SinkCollectionOperation operation, string collection, string ownPrimary, IReadOnlyCollection<GenerationContainerClaim> claims)
    {
        foreach (var claim in claims.Where(claim => claim.Destination.Value == collection))
        {
            var protectedState = claim.State is MaterializationGenerationState.Building or MaterializationGenerationState.Following or MaterializationGenerationState.Active or MaterializationGenerationState.Retired;
            var refused = operation switch
            {
                SinkCollectionOperation.Drop or SinkCollectionOperation.Rename => protectedState,
                SinkCollectionOperation.Clear => protectedState && (claim.State != MaterializationGenerationState.Building || collection != ownPrimary),
                SinkCollectionOperation.CreateIndexes => claim.State == MaterializationGenerationState.Building && collection != ownPrimary,
                _ => false
            };
            if (refused)
            {
                throw new SinkCollectionOperationRefused(operation, collection, $"generation {claim.Generation.Value} is {claim.State}");
            }
        }
    }

    /// <summary>
    /// Throw unless the collection about to be cleared is the one the sink writes to in its current phase.
    /// </summary>
    /// <param name="names">The <see cref="SinkCollectionNames"/> the sink owns.</param>
    /// <param name="isReplaying">Whether the sink is replaying.</param>
    /// <param name="collection">The collection about to be cleared.</param>
    /// <exception cref="SinkCollectionOperationRefused">Thrown when the collection is not the one the sink writes to.</exception>
    /// <remarks>
    /// During a replay only the replay shadow may be cleared; a sink that is meant to be replaying but would clear the
    /// primary has had its mode changed underneath it, and clearing would wipe the live read model.
    /// </remarks>
    public static void ThrowIfCannotClear(SinkCollectionNames names, bool isReplaying, string collection)
    {
        var owned = isReplaying ? names.Replay : names.Primary;
        if (collection != owned)
        {
            throw new SinkCollectionOperationRefused(
                SinkCollectionOperation.Clear,
                collection,
                isReplaying ? $"only the replay shadow '{names.Replay}' may be cleared during a replay" : $"only the primary '{names.Primary}' may be cleared outside a replay");
        }
    }

    /// <summary>
    /// Throw if the sink's own lifecycle would drop the primary collection.
    /// </summary>
    /// <param name="names">The <see cref="SinkCollectionNames"/> the sink owns.</param>
    /// <param name="collection">The collection about to be dropped.</param>
    /// <exception cref="SinkCollectionOperationRefused">Thrown when the collection is not the replay shadow or the promoting collection.</exception>
    public static void ThrowIfCannotDropDuringReplay(SinkCollectionNames names, string collection)
    {
        if (collection != names.Replay && collection != names.Promoting)
        {
            throw new SinkCollectionOperationRefused(
                SinkCollectionOperation.Drop,
                collection,
                $"a replay may only drop its replay shadow '{names.Replay}' or promoting collection '{names.Promoting}'");
        }
    }

    /// <summary>
    /// Throw if removing a collection by name would drop one the replay lifecycle owns.
    /// </summary>
    /// <param name="names">The <see cref="SinkCollectionNames"/> the sink owns.</param>
    /// <param name="collection">The collection about to be removed.</param>
    /// <exception cref="SinkCollectionOperationRefused">Thrown when the collection is the replay shadow or the promoting collection.</exception>
    /// <remarks>
    /// The replay shadow and the promoting collection belong to a replay in flight and only that replay may drop them;
    /// removing one by name would lose the rebuild or the promotion. Removing the primary, or a retained collection such as a
    /// revert, is what removal is for, as the <see cref="ISink"/> contract says.
    /// </remarks>
    public static void ThrowIfCannotRemove(SinkCollectionNames names, string collection)
    {
        if (collection == names.Replay || collection == names.Promoting)
        {
            throw new SinkCollectionOperationRefused(
                SinkCollectionOperation.Drop,
                collection,
                "it belongs to a replay in flight, and only that replay may drop it");
        }
    }

    /// <summary>
    /// Throw if moving the primary aside would rename it onto a collection the sink is using.
    /// </summary>
    /// <param name="names">The <see cref="SinkCollectionNames"/> the sink owns.</param>
    /// <param name="revertCollection">The collection the primary is about to be renamed to.</param>
    /// <exception cref="SinkCollectionOperationRefused">Thrown when the revert collection is the primary, the replay shadow or the promoting collection.</exception>
    public static void ThrowIfCannotMovePrimaryAside(SinkCollectionNames names, string revertCollection)
    {
        if (IsInUse(names, revertCollection))
        {
            throw new SinkCollectionOperationRefused(
                SinkCollectionOperation.Rename,
                names.Primary,
                $"the revert collection '{revertCollection}' is the primary, the replay shadow or the promoting collection of the read model");
        }
    }

    /// <summary>
    /// Throw unless the promoting collection is still the one this promotion claimed.
    /// </summary>
    /// <param name="names">The <see cref="SinkCollectionNames"/> the sink owns.</param>
    /// <param name="claimed">The collection UUID recorded when the replay shadow was claimed.</param>
    /// <param name="current">The collection UUID the promoting collection has now.</param>
    /// <exception cref="SinkCollectionOperationRefused">Thrown when either identity is unknown or they differ.</exception>
    /// <remarks>
    /// Between the claim and the rename into place another replay can drop the claimed collection and claim its own under
    /// the same name. Promoting by name alone would then put the other replay's collection in place of this one's.
    /// </remarks>
    public static void ThrowIfPromotingCollectionChanged(SinkCollectionNames names, BsonValue? claimed, BsonValue? current)
    {
        if (claimed is null || current is null || claimed.IsBsonNull || !claimed.Equals(current))
        {
            throw new SinkCollectionOperationRefused(
                SinkCollectionOperation.Rename,
                names.Promoting,
                $"it is no longer the collection this promotion claimed (claimed {claimed?.ToString() ?? "unknown"}, found {current?.ToString() ?? "none"})");
        }
    }

    static bool IsInUse(SinkCollectionNames names, string collection) =>
        collection == names.Primary || collection == names.Replay || collection == names.Promoting;
}
