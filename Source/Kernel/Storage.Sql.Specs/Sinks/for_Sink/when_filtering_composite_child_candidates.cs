// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public abstract class when_filtering_composite_child_candidates : Specification
{
    const string Tenant = "quoted'\"\\snowman☃";
    const string Version = "ac0ef349-d07d-4a70-af32-791a23883df4";
    readonly SqlReadCapture _capture = new();
    readonly List<Lookup> _narrowed = [];
    ISqlSinkHarness _harness;
    ISink _sink;
    Lookup _formatted;
    Lookup _empty;
    Lookup _encoded;

    protected abstract ISqlSinkHarness CreateHarness(SqlReadCapture capture);

    async Task Establish()
    {
        _harness = CreateHarness(_capture);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"object"}}}},"fallbackChildren":{"type":"array","items":{"type":"object","properties":{"id":{"type":"object"}}}},"emptyChildren":{"type":"array","items":{"type":"object","properties":{"id":{"type":"object"}}}},"encodedChildren":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("composite_candidates", "composite_candidates", "Composite candidates", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "composite_candidates", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        for (var index = 0; index < 12; index++)
        {
            dynamic identity = new ExpandoObject();
            identity.SEQUENCE = index == 0 ? 7.0m : 8m;
            identity.VERSION = Version.ToUpperInvariant();
            identity.SCOPE = new Dictionary<string, object?> { ["TENANT"] = index < 2 ? Tenant : $"tenant-{index}" };
            dynamic fallback = new ExpandoObject();
            fallback.SEQUENCE = identity.SEQUENCE;
            fallback.VERSION = identity.VERSION;
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
                new PropertyDifference("children", null, Children(identity)),
                new PropertyDifference("fallbackChildren", null, Children(fallback)),
                new PropertyDifference("emptyChildren", null, Children(index == 0 ? new ExpandoObject() : fallback)),
                new PropertyDifference("encodedChildren", null, Children(JsonSerializer.Serialize((object)identity)))
            ])]);
            await _sink.ApplyChanges(new Key(index.ToString(), ArrayIndexers.NoIndexers), changeset, 1UL);
        }
    }

    async Task Because()
    {
        var poco = new Identity(7, Version, new Scope(Tenant));
        foreach (var key in new object[] { poco, JsonSerializer.SerializeToNode(poco)!, JsonSerializer.SerializeToElement(poco), new IdentityConcept(poco) }) _narrowed.Add(await Find("[children].id", key));
        _formatted = await Find("[fallbackChildren].id", new NumericIdentity(7, Guid.Parse(Version)));
        _empty = await Find("[emptyChildren].id", new JsonObject());
        _encoded = await Find("[encodedChildren].id", poco);
    }

    async Task<Lookup> Find(PropertyPath path, object value)
    {
        _capture.Clear();
        var found = await _sink.TryFindRootKeyByChildValue(path, value);
        var root = found.TryGetValue(out var key) ? key.Value?.ToString() : null;
        return new Lookup(root, _capture.Rows, _capture.Commands.Any(command => command.Contains("WHERE", StringComparison.OrdinalIgnoreCase)), _capture.Commands.Any(command => command.Contains(Tenant, StringComparison.Ordinal)));
    }

    static List<ExpandoObject> Children(object identity)
    {
        dynamic child = new ExpandoObject();
        child.id = identity;
        return [(ExpandoObject)child];
    }

    void Destroy() => _harness.Dispose();

    [Fact] public void should_find_all_four_key_materializations_despite_property_case_and_leaf_format_differences() => _narrowed.Select(result => result.Root).ShouldEqual(new string?[] { "0", "0", "0", "0" });
    [Fact] public void should_apply_a_sql_predicate_to_all_four_key_materializations() => _narrowed.TrueForAll(result => result.HasPredicate).ShouldBeTrue();
    [Fact] public void should_materialize_only_the_two_string_leaf_candidates_for_each_lookup() => _narrowed.Select(result => result.Rows).ShouldEqual(new[] { 2, 2, 2, 2 });
    [Fact] public void should_parameterize_the_escaped_string_leaf() => _narrowed.Exists(result => result.ContainsLiteral).ShouldBeFalse();
    [Fact] public void should_find_numeric_and_guid_leaves_with_different_formats() => _formatted.Root.ShouldEqual("0");
    [Fact] public void should_fall_back_for_numeric_and_guid_only_keys() => _formatted.Rows.ShouldEqual(12);
    [Fact] public void should_find_an_empty_composite_key() => _empty.Root.ShouldEqual("0");
    [Fact] public void should_fall_back_when_the_key_has_no_leaf() => _empty.Rows.ShouldEqual(12);
    [Fact] public void should_keep_composite_identities_stored_as_json_text_candidates() => _encoded.Root.ShouldEqual("0");

    record Scope(string Tenant);
    record Identity(long Sequence, string Version, Scope Scope) : IComparable
    {
        public static bool operator <(Identity left, Identity right) => left.CompareTo(right) < 0;
        public static bool operator <=(Identity left, Identity right) => left.CompareTo(right) <= 0;
        public static bool operator >(Identity left, Identity right) => left.CompareTo(right) > 0;
        public static bool operator >=(Identity left, Identity right) => left.CompareTo(right) >= 0;
        public int CompareTo(object? obj) => obj is Identity other ? (Sequence, Version, Scope.Tenant).CompareTo((other.Sequence, other.Version, other.Scope.Tenant)) : 1;
    }
    record NumericIdentity(long Sequence, Guid Version);
    record IdentityConcept(Identity Value) : ConceptAs<Identity>(Value);
    record Lookup(string? Root, int Rows, bool HasPredicate, bool ContainsLiteral);
}
