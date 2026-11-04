// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Tracks the pipeline updates that <see cref="AppliedEventMarkerUpdate"/> produced, so a consumer can tell them from any other pipeline.
/// </summary>
internal static class AppliedEventMarkerUpdates
{
    static readonly ConditionalWeakTable<UpdateDefinition<BsonDocument>, object> _marked = new();

    /// <summary>
    /// Registers an update as produced by <see cref="AppliedEventMarkerUpdate"/>.
    /// </summary>
    /// <param name="update">The update.</param>
    /// <returns>The same update.</returns>
    public static UpdateDefinition<BsonDocument> Mark(UpdateDefinition<BsonDocument> update)
    {
        _marked.AddOrUpdate(update, true);
        return update;
    }

    /// <summary>
    /// Gets whether an update was produced by <see cref="AppliedEventMarkerUpdate"/>.
    /// </summary>
    /// <param name="update">The update.</param>
    /// <returns>True when it was.</returns>
    public static bool IsMarked(UpdateDefinition<BsonDocument> update) => _marked.TryGetValue(update, out _);
}
