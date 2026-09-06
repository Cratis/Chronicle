// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.given;

public class a_storage_with_patterns_for_two_scopes : Specification
{
    protected BehaviorPatternStorage _storage;
    protected BehaviorPattern _mondayMorning;
    protected BehaviorPattern _monday;
    protected BehaviorPattern _forSomebodyElse;

    async Task Establish()
    {
        _storage = new();

        _mondayMorning = Pattern("user-42", [new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _monday = Pattern("user-42", [new Facet(FacetName.Day, "Monday")]);
        _forSomebodyElse = Pattern("user-7", [new Facet(FacetName.Day, "Monday")]);

        await _storage.Save([_mondayMorning, _monday, _forSomebodyElse]);
    }

    protected static BehaviorPattern Pattern(PatternGroupingKey groupingKey, IEnumerable<Facet> facets, double confidence = 0.9d) =>
        new(groupingKey, new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
