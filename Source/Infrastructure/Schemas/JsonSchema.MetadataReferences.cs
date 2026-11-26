// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    const int MaxMetadataReferenceStates = 4096;

    JsonObject MetadataOrigin => ReferenceEquals(ResolutionScope.OriginRoot, Root.Node)
        ? Root.ResourceIndex.Origins.GetValueOrDefault(ResolutionScope.Origin, Node)
        : Node;

    static bool RecursiveAnchorIsValid(JsonObject node) => !node.ContainsKey("$recursiveAnchor") || IsBooleanSchema(node["$recursiveAnchor"]);

    MetadataReach ReachMetadata(bool includeChildren) => new MetadataTraversal(includeChildren).Walk(this);

    JsonSchema MetadataView(JsonObject node) => new(node, Root, new(Root.ResourceIndex.Scopes.GetValueOrDefault(node, ResourceBase), node.GetPath(), [], Root.Node));

    JsonObject MetadataChildOrigin(JsonObject node)
    {
        var index = Root.ResourceIndex;
        if (index.Scopes.ContainsKey(node)) return node;
        JsonNode ancestor = node;
        while (ancestor.Parent is { } parent) ancestor = parent;
        if (!ReferenceEquals(ancestor, Node)) return node;
        var origin = ResolutionScope.Origin + node.GetPath()[1..];
        return index.Origins.GetValueOrDefault(origin, node);
    }

    JsonObject? MetadataTarget(string reference, bool scoped)
    {
        // Ordinary references keep the historical root-pointer precedence, without cloned wrappers.
        if (!scoped && reference.StartsWith('#') && ResolveResourcePointer(Root.Node, reference[1..]) is { } legacy) return legacy;
        var separator = reference.IndexOf('#');
        var resource = separator < 0 ? reference : reference[..separator];
        var fragment = separator < 0 ? string.Empty : reference[(separator + 1)..];
        var index = Root.ResourceIndex;
        if (!Uri.TryCreate(ResourceBase, resource, out var uri) || !index.Roots.TryGetValue(uri.AbsoluteUri, out var root)) return null;
        if (fragment.Length == 0) return root;
        return fragment.StartsWith('/') ? ResolveResourcePointer(root, fragment) : index.Anchors.GetValueOrDefault((uri.AbsoluteUri, fragment));
    }

    IEnumerable<JsonObject> MetadataChildren(bool includeChildren, bool contextual)
    {
        if (includeChildren && contextual)
        {
            return SchemaChildren(Node, includeDefinitions: false).Select(child => child.Node).OfType<JsonObject>();
        }
        var compositions = contextual ? CompositionNodes("allOf").Concat(CompositionNodes("anyOf")).Concat(CompositionNodes("oneOf")) : LegacyMetadataCompositions();
        if (!includeChildren) return contextual ? compositions : [];
        var properties = (Node["properties"] as JsonObject)?.Select(entry => entry.Value).OfType<JsonObject>() ?? [];
        return properties.Concat(compositions).Concat(new[] { Node["items"], Node["additionalProperties"] }.OfType<JsonObject>());
    }

    IEnumerable<JsonObject> CompositionNodes(string keyword) => (Node[keyword] as JsonArray)?.OfType<JsonObject>() ?? [];

    IEnumerable<JsonObject> LegacyMetadataCompositions()
    {
        var allOf = CompositionNodes("allOf").ToArray();
        if (allOf.Length > 0) return allOf;
        return CompositionNodes("anyOf").Where(node =>
        {
            var candidate = MetadataView(node);
            if (candidate.Type == JsonObjectType.Null) return false;
            var target = StringValue(node, "$ref") is { } reference ? candidate.MetadataTarget(reference, scoped: false) : null;
            return target is null || MetadataView(target).Type != JsonObjectType.Null;
        }).Take(1);
    }

    /// <summary>
    /// Carries concrete annotations separately from uncertainty; uncertainty never invents a handler.
    /// </summary>
    internal sealed class MetadataReach
    {
        /// <summary>
        /// Gets the unique reachable schema nodes.
        /// </summary>
        internal List<JsonSchema> Schemas { get; } = [];

        /// <summary>
        /// Gets or sets whether traversal or resource indexing was incomplete.
        /// </summary>
        internal bool Indeterminate { get; set; }

        /// <summary>
        /// Gets or sets whether a contextual reference was visited.
        /// </summary>
        internal bool Contextual { get; set; }

        /// <summary>
        /// Gets or sets the number of unique scheduled nodes, bounded by the traversal budget.
        /// </summary>
        internal int UniqueNodes { get; set; }

        /// <summary>
        /// Gets or sets the number of anchor groups expanded, at most once per group.
        /// </summary>
        internal int AnchorExpansions { get; set; }
    }

    sealed class MetadataTraversal(bool includeChildren)
    {
        readonly MetadataReach _reach = new();
        readonly Queue<(JsonSchema Schema, int Depth)> _pending = [];
        readonly HashSet<JsonObject> _visited = new(ReferenceEqualityComparer.Instance);
        readonly HashSet<string> _expandedAnchors = new(StringComparer.Ordinal);
        bool _recursiveExpanded;
        bool _unresolvedStatic;
        int _nextDepth;

        internal MetadataReach Walk(JsonSchema origin)
        {
            var index = origin.Root.ResourceIndex;
            _reach.Indeterminate = index.Truncated || index.IndeterminateResources.Count > 0;
            Enqueue(origin, origin.MetadataOrigin, origin);
            while (_pending.TryDequeue(out var step))
            {
                _nextDepth = step.Depth + 1;
                Visit(step.Schema, index.Contextual);
            }
            _reach.Indeterminate |= _reach.Contextual && _unresolvedStatic;
            _reach.UniqueNodes = _visited.Count;
            return _reach;
        }

        void Visit(JsonSchema schema, bool contextualDocument)
        {
            _reach.Schemas.Add(schema);
            _reach.Contextual |= schema.HasContextualReference;
            AddReference(schema, "$ref", contextualDocument);
            if (schema.Node.ContainsKey("$dynamicRef")) AddDynamicTargets(schema);
            if (schema.Node.ContainsKey("$recursiveRef")) AddRecursiveTargets(schema);
            AddChildren(schema, contextualDocument);
        }

        void AddChildren(JsonSchema schema, bool contextualDocument)
        {
            var cached = schema.CachedMetadataChildren(includeChildren, contextualDocument);
            AddCachedChildren(schema, cached);
            AddUncachedChildren(schema, cached, contextualDocument);
        }

        void AddCachedChildren(JsonSchema schema, Dictionary<JsonObject, JsonSchema> cached)
        {
            foreach (var child in cached)
            {
                if (!Enqueue(schema, child.Key, child.Value)) break;
            }
        }

        void AddUncachedChildren(JsonSchema schema, Dictionary<JsonObject, JsonSchema> cached, bool contextualDocument)
        {
            foreach (var child in schema.MetadataChildren(includeChildren, contextualDocument))
            {
                var origin = schema.MetadataChildOrigin(child);
                if (!Enqueue(schema, origin, cached.GetValueOrDefault(origin))) break;
            }
        }

        bool Enqueue(JsonSchema parent, JsonObject node, JsonSchema? cached = null)
        {
            if (!parent.Root.ResourceIndex.Contextual && _nextDepth > MaxResourceTraversalDepth) return true;
            node = parent.MetadataChildOrigin(node);
            if (_visited.Contains(node)) return true;
            if (_visited.Count >= MaxMetadataReferenceStates)
            {
                _reach.Indeterminate = true;
                return false;
            }
            _visited.Add(node);
            _pending.Enqueue((cached ?? parent.MetadataView(node), _nextDepth));
            return true;
        }

        JsonObject? AddReference(JsonSchema schema, string keyword, bool scoped)
        {
            if (!schema.Node.ContainsKey(keyword)) return null;
            var reference = StringValue(schema.Node, keyword);
            var target = reference is null ? null : schema.MetadataTarget(reference, scoped);
            if (target is null)
            {
                _unresolvedStatic |= keyword == "$ref";
                _reach.Indeterminate |= keyword != "$ref";
            }
            else
            {
                Enqueue(schema, target, keyword == "$ref" ? schema._referenceCache?.Value : null);
            }
            return target;
        }

        void AddDynamicTargets(JsonSchema schema)
        {
            var target = AddReference(schema, "$dynamicRef", scoped: true);
            if (target is null) return;
            var reference = StringValue(schema.Node, "$dynamicRef")!;
            var separator = reference.IndexOf('#');
            var fragment = separator < 0 ? string.Empty : reference[(separator + 1)..];

            // Draft 2020-12 Core 8.2.3.2: pointers and ordinary anchors do not rebind.
            if (fragment != StringValue(target, "$dynamicAnchor") || !_expandedAnchors.Add(fragment)) return;
            _reach.AnchorExpansions++;
            if (!schema.Root.ResourceIndex.DynamicGroups.TryGetValue(fragment, out var group)) return;
            foreach (var candidate in group) Enqueue(schema, candidate);
        }

        void AddRecursiveTargets(JsonSchema schema)
        {
            // Draft 2019-09 Core 8.2.4.2 supports only '#'. Preserve its static target too.
            if (StringValue(schema.Node, "$recursiveRef") != "#")
            {
                _reach.Indeterminate = true;
                return;
            }
            var target = AddReference(schema, "$recursiveRef", scoped: true);
            if (target is null) return;
            if (!IsRecursiveRoot(target)) return;
            if (_recursiveExpanded) return;
            _recursiveExpanded = true;
            _reach.AnchorExpansions++;
            foreach (var candidate in schema.Root.ResourceIndex.RecursiveAnchors) Enqueue(schema, candidate);
        }
    }
}
