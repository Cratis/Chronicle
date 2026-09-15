// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet;

/// <summary>
/// A facet set crosses serialization boundaries wherever a pattern does - a grain call, a copier, a wire. The
/// serializer binds the deserialization constructor by parameter name and type, so the set must offer one whose
/// parameter matches the property exactly - and canonicalization re-runs on the way back in, so the key holds.
/// </summary>
public class when_round_tripping_through_json : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    FacetSet _original;
    FacetSet _roundTripped;

    void Establish() => _original = new FacetSet(
    [
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.Day, "Monday"),
        new Facet(FacetName.TimeBucket, "Morning")
    ]);

    void Because() => _roundTripped = JsonSerializer.Deserialize<FacetSet>(JsonSerializer.Serialize(_original, _options), _options)!;

    [Fact] void should_hold_the_same_facets() => _roundTripped.Facets.SequenceEqual(_original.Facets).ShouldBeTrue();
    [Fact] void should_rebuild_the_same_key() => _roundTripped.Key.ShouldEqual(_original.Key);
}
