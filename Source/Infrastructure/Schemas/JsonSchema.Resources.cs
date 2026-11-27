// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    const int MaxResourceTraversalDepth = 64;
    static readonly Uri _anonymousResourceBase = new("urn:cratis:schema:root");
    readonly Uri? _resourceBase;
    readonly ResolutionContext? _resolutionContext;
    volatile Cached<ResourceIndexContext>? _resourceIndex;
    volatile Cached<IReadOnlyList<JsonSchema>>? _referenceChain;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchema"/> class in a resolved resource scope.
    /// </summary>
    /// <param name="node">The schema node.</param>
    /// <param name="root">The document root.</param>
    /// <param name="context">The origin and entered resource scope, with the node's identifier already applied.</param>
    internal JsonSchema(JsonObject node, JsonSchema root, ResolutionContext context)
    {
        Node = node;
        _root = root;
        _resourceBase = context.ResourceBase;
        _resolutionContext = context;
    }

    /// <summary>
    /// Gets this schema and up to 64 resolved reference hops, stopping at a cycle or unresolved reference.
    /// </summary>
    internal IReadOnlyList<JsonSchema> ReferenceChain => (_referenceChain ??= new(BuildReferenceChain())).Value;

    JsonSchema ResolvedReference => ReferenceChain[^1];

    Uri ResourceBase => _resourceBase ?? ResourceId(Root.Node, _anonymousResourceBase);

    ResourceIndexContext ResourceIndex
    {
        get
        {
            var cached = _resourceIndex;
            if (cached is not null) return cached.Value;
            var index = new ResourceIndexContext();
            IndexResources(Node, index);
            return (_resourceIndex = new(index)).Value;
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

    static void IndexResources(JsonObject root, ResourceIndexContext context)
    {
        var pending = new Queue<(JsonObject Node, Uri Inherited)>();
        context.ExaminedEdges = 1;
        pending.Enqueue((root, _anonymousResourceBase));
        while (pending.TryDequeue(out var current))
        {
            var scope = IndexResourceNode(current.Node, current.Inherited, context);
            IndexChildren(current.Node, scope, context, pending);
        }
    }

    static Uri IndexResourceNode(JsonObject node, Uri inherited, ResourceIndexContext context)
    {
        var scope = ResourceId(node, inherited);
        context.Scopes.Add(node, scope);
        context.Origins[node.GetPath()] = node;
        if (context.Scopes.Count == 1 || scope != inherited) context.Roots.TryAdd(scope.AbsoluteUri, node);
        IndexAnchor(node, scope, context);
        IndexDynamicAnchor(node, scope, context);
        context.Contextual |= node.ContainsKey("$dynamicRef") || node.ContainsKey("$recursiveRef");
        if (!RecursiveAnchorIsValid(node)) context.IndeterminateResources.Add(scope.AbsoluteUri);
        if (IsRecursiveRoot(node)) context.RecursiveAnchors.Add(node);
        return scope;
    }

    static void IndexChildren(JsonObject node, Uri scope, ResourceIndexContext context, Queue<(JsonObject Node, Uri Inherited)> pending)
    {
        foreach (var child in SchemaChildren(node))
        {
            if (!ConsumeResourceEdge(context)) break;
            if (child.Optional || IsBooleanSchema(child.Node)) continue;
            if (child.Node is not JsonObject schema)
            {
                context.Truncated = true;
                continue;
            }
            pending.Enqueue((schema, scope));
        }
    }

    static bool ConsumeResourceEdge(ResourceIndexContext context)
    {
        if (context.ExaminedEdges >= MaxMetadataReferenceStates)
        {
            context.Truncated = true;
            return false;
        }

        context.ExaminedEdges++;
        return true;
    }

    static void IndexAnchor(JsonObject node, Uri resource, ResourceIndexContext context)
    {
        if (node["$anchor"] is not JsonValue value || !value.TryGetValue<string>(out var anchor)) return;
        var key = (resource.AbsoluteUri, anchor);
        if (context.DynamicAnchors.TryGetValue(key, out var dynamicAnchor) && !ReferenceEquals(dynamicAnchor, node))
        {
            context.IndeterminateResources.Add(resource.AbsoluteUri);
        }
        context.Anchors.TryAdd(key, node);
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

    JsonSchema CreateChildSchema(JsonObject node) => new((JsonObject)node.DeepClone(), Root, ChildResolutionContext(node));

    JsonSchemaProperty CreateChildProperty(string name, JsonObject node) => new(name, (JsonObject)node.DeepClone(), Root, ChildResolutionContext(node));

    JsonSchema CreateResolvedSchema(JsonObject target) =>
        new((JsonObject)target.DeepClone(), Root, EnterResource(Root.ResourceIndex.Scopes.GetValueOrDefault(target, Root.ResourceBase), target.GetPath(), Root.ResolutionScope.OriginRoot));

    JsonSchema? ResolveScopedRef(string reference)
    {
        // Preserve the root's legacy fragment precedence; embedded resources get first refusal in their own scope.
        if (ResourceBase != Root.ResourceBase && ResolveResourceRef(reference) is { } scoped) return scoped;
        var target = Root.ResolveRootRef(reference) ?? Root.ResolveResourceRef(reference);
        return target is null ? null : new((JsonObject)target.Node.DeepClone(), Root, EnterResource(target.ResourceBase, target.ResolutionScope.Origin, target.ResolutionScope.OriginRoot));
    }

    List<JsonSchema> BuildReferenceChain()
    {
        var chain = new List<JsonSchema> { this };
        var visited = new HashSet<(Uri Resource, string Reference)>();
        var current = this;
        for (var hop = 0; hop < MaxResourceTraversalDepth; hop++)
        {
            if (current.ReferenceValue is not { } reference || !visited.Add((current.ResourceBase, reference))) break;
            if (current.Reference is not { } target) break;
            chain.Add(target);
            current = target;
        }
        return chain;
    }

    sealed class ResourceIndexContext
    {
        internal Dictionary<string, JsonObject> Roots { get; } = new(StringComparer.Ordinal);
        internal Dictionary<(string Resource, string Anchor), JsonObject> Anchors { get; } = [];
        internal Dictionary<(string Resource, string Anchor), JsonObject> DynamicAnchors { get; } = [];
        internal Dictionary<JsonObject, Uri> Scopes { get; } = [];
        internal Dictionary<string, JsonObject> Origins { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, List<JsonObject>> DynamicGroups { get; } = new(StringComparer.Ordinal);
        internal List<JsonObject> RecursiveAnchors { get; } = [];
        internal bool Contextual { get; set; }
        internal HashSet<string> IndeterminateResources { get; } = [];
        internal bool Truncated { get; set; }
        internal int ExaminedEdges { get; set; }
    }
}
