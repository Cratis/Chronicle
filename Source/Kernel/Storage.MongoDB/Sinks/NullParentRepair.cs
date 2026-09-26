// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Repairs legacy null parents after MongoDB rejects a dotted update. Never writes values from the read-model state.
/// </summary>
internal static class NullParentRepair
{
    /// <summary>The MongoDB error raised when a dotted update meets a null parent.</summary>
    internal const int CannotCreateField = 28;

    /// <summary>
    /// Unsets null ancestors of dotted updates and retries the original write exactly once.
    /// </summary>
    /// <param name="collection">The collection containing the legacy document.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="update">The rejected update.</param>
    /// <param name="retry">The original write, preserving its filter, options and array filters.</param>
    /// <returns>A task completing when the repaired write has been retried.</returns>
    internal static async Task RepairAndRetry(IMongoCollection<BsonDocument> collection, BsonValue id, UpdateDefinition<BsonDocument> update, Func<Task> retry)
    {
        var rendered = update.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
        var ancestors = new HashSet<string>();

        foreach (var operation in rendered.AsBsonDocument.Elements.Where(element => element.Name != "$unset"))
        {
            if (operation.Value is not BsonDocument fields)
            {
                continue;
            }

            foreach (var field in fields.Elements)
            {
                var segments = field.Name.Split('.');
                for (var length = 1; length < segments.Length; length++)
                {
                    // A positional array path needs a filter scoped to that specific element. Never unset
                    // an unrelated element merely because another member of the array is null.
                    if (segments.Take(length).Any(segment => segment.StartsWith('$')))
                    {
                        break;
                    }

                    ancestors.Add(string.Join('.', segments.Take(length)));
                }
            }
        }

        // Repair the outermost parent first. The existence condition prevents Eq(null) from
        // matching an absent field and never touches a non-null parent.
        foreach (var ancestor in ancestors.OrderBy(path => path.Count(character => character == '.')))
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                Builders<BsonDocument>.Filter.Eq(ancestor, BsonNull.Value),
                Builders<BsonDocument>.Filter.Exists(ancestor, true));
            await collection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Unset(ancestor));
        }

        await retry();
    }
}
