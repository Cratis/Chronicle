// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

public partial class InMemorySink
{
    void StageRootJoins(StagedWrite staged, Joined[] rootJoins)
    {
        if (rootJoins.Length == 0)
        {
            return;
        }

        var selectors = rootJoins.Where(joined => joined.Changes.Any())
            .Select(joined => (Join: joined, Path: joined.OnProperty.Segments.ToArray())).ToArray();
        foreach (var rowKey in staged.RowKeys())
        {
            var matching = MatchingRootJoins(staged.Current(rowKey)!, selectors);
            if (matching is null)
            {
                continue;
            }

            var candidate = staged.CandidateFor(rowKey);
            var hasIdentity = ((IDictionary<string, object?>)candidate).TryGetValue("id", out var identity);
            foreach (var joined in matching)
            {
                candidate = ApplyActualChanges(new Key(rowKey, ArrayIndexers.NoIndexers), joined.Changes, candidate);
            }

            if (hasIdentity)
            {
                // A mapping may set the id to a different value; the row keeps the identity it is stored under.
                ((IDictionary<string, object?>)candidate)["id"] = identity;
            }

            staged.Stage(rowKey, candidate, advancesWatermark: true);
        }
    }

    List<Joined>? MatchingRootJoins(ExpandoObject current, (Joined Join, IPropertyPathSegment[] Path)[] selectors)
    {
        List<Joined>? matching = null;
        foreach (var (joined, path) in selectors)
        {
            if (TryFindValueInDocument(current, path, 0, joined.Key))
            {
                (matching ??= []).Add(joined);
            }
        }

        return matching;
    }
}
