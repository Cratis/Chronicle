// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    const int MaxResourceTraversalDepth = 64;
    static readonly Uri _anonymousResourceBase = new("urn:cratis:schema:root");
    static readonly string[] _resourceSchemaMaps = ["$defs", "definitions", "properties"];
    static readonly string[] _resourceSchemaArrays = ["allOf", "anyOf", "oneOf"];
    static readonly string[] _resourceSingleSchemas = ["items", "additionalProperties", "not"];
    readonly Uri? _resourceBase;
    volatile Cached<(Dictionary<string, JsonObject> Roots, Dictionary<(string Resource, string Anchor), JsonObject> Anchors, Dictionary<JsonObject, Uri> Scopes)>? _resourceIndex;
    volatile Cached<IReadOnlyList<JsonSchema>>? _referenceChain;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchema"/> class in a resolved resource scope.
    /// </summary>
    /// <param name="node">The schema node.</param>
    /// <param name="root">The document root.</param>
    /// <param name="resourceBase">The effective base, with the node's identifier already applied.</param>
    internal JsonSchema(JsonObject node, JsonSchema root, Uri resourceBase)
    {
        Node = node;
        _root = root;
        _resourceBase = resourceBase;
    }

    /// <summary>
    /// Gets this schema and up to 64 resolved reference hops, stopping at a cycle or unresolved reference.
    /// </summary>
    internal IReadOnlyList<JsonSchema> ReferenceChain => (_referenceChain ??= new(BuildReferenceChain())).Value;

    JsonSchema ResolvedReference => ReferenceChain[^1];

    Uri ResourceBase => _resourceBase ?? ResourceId(Root.Node, _anonymousResourceBase);

    (Dictionary<string, JsonObject> Roots, Dictionary<(string Resource, string Anchor), JsonObject> Anchors, Dictionary<JsonObject, Uri> Scopes) ResourceIndex
    {
        get
        {
            var cached = _resourceIndex;
            if (cached is not null) return cached.Value;
            var roots = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            var anchors = new Dictionary<(string Resource, string Anchor), JsonObject>();
            var scopes = new Dictionary<JsonObject, Uri>();
            IndexResources(Node, _anonymousResourceBase, new(roots, anchors, scopes), 0);
            return (_resourceIndex = new((roots, anchors, scopes))).Value;
        }
    }

    static JsonObject? ResolveResourcePointer(JsonObject root, string fragment)
    {
        JsonNode? current = root;
        foreach (var rawSegment in fragment.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = rawSegment.Replace("~1", "/").Replace("~0", "~");
            current = current switch
            {
                JsonObject obj => obj[segment],
                JsonArray array when int.TryParse(segment, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null
            };
            if (current is null) return null;
        }
        return current as JsonObject;
    }

    static Uri ResourceId(JsonObject node, Uri inherited)
    {
        if (node["$id"] is not JsonValue value || !value.TryGetValue<string>(out var id)) return inherited;
        return ResolveResourceId(inherited, id);
    }

    static Uri ResolveResourceId(Uri inherited, string id) =>
        Uri.TryCreate(inherited, id, out var resource) && string.IsNullOrEmpty(resource.Fragment) ? resource : inherited;

    static void IndexResources(JsonObject node, Uri inherited, ResourceIndexContext context, int depth)
    {
        if (depth > MaxResourceTraversalDepth || context.Scopes.ContainsKey(node)) return;
        var scope = ResourceId(node, inherited);
        context.Scopes.Add(node, scope);
        if (depth == 0 || scope != inherited) context.Roots.TryAdd(scope.AbsoluteUri, node);
        if (node["$anchor"] is JsonValue value && value.TryGetValue<string>(out var anchor))
        {
            context.Anchors.TryAdd((scope.AbsoluteUri, anchor), node);
        }
        foreach (var child in ResourceSchemaChildren(node))
        {
            IndexResources(child, scope, context, depth + 1);
        }
    }

    static IEnumerable<JsonObject> ResourceSchemaChildren(JsonObject node)
    {
        // Do not interpret instance annotations (default, examples, enum, const) as schema declarations.
        var maps = _resourceSchemaMaps.Select(key => node[key]).OfType<JsonObject>()
            .SelectMany(map => map.Select(_ => _.Value).OfType<JsonObject>());
        var arrays = _resourceSchemaArrays.Select(key => node[key]).OfType<JsonArray>()
            .SelectMany(array => array.OfType<JsonObject>());
        var singles = _resourceSingleSchemas.Select(key => node[key]).OfType<JsonObject>();
        return maps.Concat(arrays).Concat(singles);
    }

    JsonSchema ResolveTypeReference(JsonSchema fallback) => HasReference && Reference is null ? fallback : ResolvedReference;

    JsonSchema? ResolveResourceRef(string reference)
    {
        var separator = reference.IndexOf('#');
        var resource = separator < 0 ? reference : reference[..separator];
        var fragment = separator < 0 ? string.Empty : reference[(separator + 1)..];
        var index = Root.ResourceIndex;
        if (!Uri.TryCreate(ResourceBase, resource, out var targetUri) ||
            !index.Roots.TryGetValue(targetUri.AbsoluteUri, out var resourceRoot))
        {
            return null;
        }
        JsonObject? target;
        if (fragment.Length == 0) target = resourceRoot;
        else if (fragment.StartsWith('/')) target = ResolveResourcePointer(resourceRoot, fragment);
        else target = index.Anchors.GetValueOrDefault((targetUri.AbsoluteUri, fragment));
        return target is null ? null : CreateResolvedSchema(target);
    }

    JsonSchema CreateChildSchema(JsonObject node) => new(node, Root, ResourceId(node, ResourceBase));

    JsonSchemaProperty CreateChildProperty(string name, JsonObject node) => new(name, node, Root, ResourceId(node, ResourceBase));

    JsonSchema CreateResolvedSchema(JsonObject target) =>
        new((JsonObject)target.DeepClone(), Root, Root.ResourceIndex.Scopes.GetValueOrDefault(target, Root.ResourceBase));

    JsonSchema? ResolveScopedRef(string reference)
    {
        // Preserve the root's legacy fragment precedence; embedded resources get first refusal in their own scope.
        if (ResourceBase != Root.ResourceBase && ResolveResourceRef(reference) is { } scoped) return scoped;
        return Root.ResolveRootRef(reference) ?? Root.ResolveResourceRef(reference);
    }

    List<JsonSchema> BuildReferenceChain()
    {
        var chain = new List<JsonSchema> { this };
        var visited = new HashSet<(Uri Resource, string Reference)>();
        var current = this;
        for (var hop = 0; hop < MaxResourceTraversalDepth; hop++)
        {
            if (current.Node["$ref"]?.GetValue<string>() is not { } reference || !visited.Add((current.ResourceBase, reference))) break;
            if (current.Reference is not { } target) break;
            chain.Add(target);
            current = target;
        }
        return chain;
    }

    sealed record ResourceIndexContext(
        Dictionary<string, JsonObject> Roots,
        Dictionary<(string Resource, string Anchor), JsonObject> Anchors,
        Dictionary<JsonObject, Uri> Scopes);
}
