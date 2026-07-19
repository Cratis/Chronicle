// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_OptionalReadModelValues;

/// <summary>
/// Reproduces the CHR-16 symptom at the real release path: a projection that only ever set <c>Id</c> and
/// <c>PlacedAt</c> (the source event for the optional <c>CompletedAt</c> never fired) stores a document with
/// no <c>CompletedAt</c> field. Releasing that document through the schema-driven converter must NOT fabricate
/// a type-default sentinel (<c>0001-01-01</c>) for the unset optional — it must stay null/absent.
/// </summary>
public class when_releasing_a_read_model_with_an_unset_optional : given.a_schema_driven_read_model_release
{
    record OrderTimeline(Guid Id, DateTimeOffset PlacedAt, DateTimeOffset? CompletedAt);

    JsonObject _result;

    void Because()
    {
        var schema = _generator.Generate(typeof(OrderTimeline));

        var stored = new ExpandoObject();
        var storedAsDictionary = (IDictionary<string, object?>)stored;
        storedAsDictionary[nameof(OrderTimeline.Id)] = Guid.NewGuid();
        storedAsDictionary[nameof(OrderTimeline.PlacedAt)] = DateTimeOffset.UtcNow;

        _result = _converter.ToJsonObject(stored, schema);
    }

    [Fact] void should_keep_the_property_that_was_set() => _result.ContainsKey(nameof(OrderTimeline.PlacedAt)).ShouldBeTrue();
    [Fact] void should_not_materialize_the_unset_optional() => _result.ContainsKey(nameof(OrderTimeline.CompletedAt)).ShouldBeFalse();
}
