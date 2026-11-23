// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValueInRoot(PropertyPath childPropertyPath, object childValue, Key rootKey)
    {
        if (childValue is null) return Option<Key>.None();
        var segments = childPropertyPath.Segments.ToArray();
        if (segments.Length == 0) return Option<Key>.None();
        await EnsureLayout();
        var rootColumn = FindColumn(segments[0].Value);
        if (rootColumn?.IsJson != true) return Option<Key>.None();
        var keyColumn = _columns.FirstOrDefault(column => column.IsKey);
        if (keyColumn is null) return Option<Key>.None();
        var id = GetIdString(rootKey);
        if (keyColumn.ClrType == typeof(Guid) && !Guid.TryParse(id, out _)) return Option<Key>.None();

        await using var scope = await OpenActiveScope();
        var entity = await scope.DbContext.Entries.AsNoTracking().FirstOrDefaultAsync(BuildIdPredicate(id));
        if (entity is null || !ContainsChildValue(entity, rootColumn, segments, childValue)) return Option<Key>.None();
        return new Option<Key>(new Key(entity[keyColumn.Name]!, ArrayIndexers.NoIndexers));
    }
}
