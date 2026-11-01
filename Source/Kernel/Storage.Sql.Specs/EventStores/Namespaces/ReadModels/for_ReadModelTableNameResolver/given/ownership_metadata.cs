// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.given;

internal sealed class ownership_metadata : IReadModelTableOwnership
{
    internal readonly List<ReadModelTableClaim> Claims = [];
    internal bool ShapeMatches = true;
    internal int ShapeChecks;
    internal Exception? Failure;

    public Task<IReadOnlyList<ReadModelTableClaim>> Read()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
        return Task.FromResult<IReadOnlyList<ReadModelTableClaim>>(Claims.ToArray());
    }

    public Task<bool> HasExpectedShape(string physicalName)
    {
        ShapeChecks++;
        return Task.FromResult(ShapeMatches);
    }
}
