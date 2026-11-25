// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public async Task<bool> TryReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema expected, JsonSchema schema)
    {
        await using var scope = await database.EventStore(eventStore);
        var id = eventTypeId.Value;
        var existing = await scope.DbContext.EventTypes.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == id);
        if (existing is null || !existing.Schemas.TryGetValue(generation, out var stored))
        {
            return false;
        }

        if ((await JsonSchema.FromJsonAsync(stored)).ToJson() != expected.ToJson())
        {
            return false;
        }

        var observed = existing.Schemas;
        var replacement = new Dictionary<uint, string>(observed) { [generation] = schema.ToJson() };

        // Compare the entire serialized map so a concurrent update of another generation is preserved too.
        var changed = await scope.DbContext.EventTypes.Where(_ => _.Id == id && _.Schemas == observed)
            .ExecuteUpdateAsync(_ => _.SetProperty(item => item.Schemas, replacement));
        return changed > 0;
    }
}
