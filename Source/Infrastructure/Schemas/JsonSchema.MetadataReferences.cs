// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    const int MaxMetadataReferenceStates = 4096;

    /// <summary>
    /// Gets the conservative, document-local reference graph for metadata, independently of type selection.
    /// </summary>
    internal MetadataReach MetadataReferences => ReachMetadata(includeChildren: false);

    /// <summary>
    /// Gets metadata reach through references and supported child schemas, sharing one traversal budget.
    /// </summary>
    internal MetadataReach MetadataGraph => ReachMetadata(includeChildren: true);

    static bool ContainsContextualReference(JsonObject node, int depth = 0) =>
        node.ContainsKey("$dynamicRef") || node.ContainsKey("$recursiveRef") ||
        (depth < MaxResourceTraversalDepth && ResourceSchemaChildren(node).Any(child => ContainsContextualReference(child, depth + 1)));

    static bool RecursiveAnchorIsValid(JsonObject node) => !node.ContainsKey("$recursiveAnchor") ||
        (node["$recursiveAnchor"] is JsonValue value && value.TryGetValue<bool>(out _));

    MetadataReach ReachMetadata(bool includeChildren) => new MetadataTraversal(includeChildren).Walk(this);

    IEnumerable<JsonSchema> MetadataChildren(bool contextual)
    {
        // Retain the legacy composition selection for ordinary schemas. Contextual branches need
        // conservative reach, not a first-branch type guess, including oneOf and anyOf alternatives.
        var compositions = contextual || ContainsContextualReference(Node)
            ? AllOf.Concat(AnyOf).Concat(OneOf)
            : LegacyMetadataCompositions();
        return Properties.Values.Concat(compositions)
            .Concat(new[] { Item, AdditionalPropertiesSchema }.OfType<JsonSchema>());
    }

    IEnumerable<JsonSchema> LegacyMetadataCompositions() => AllOf.Count > 0
        ? AllOf
        : AnyOf.Where(candidate => candidate.Type != JsonObjectType.Null &&
            !(candidate.HasReference && candidate.Reference?.Type == JsonObjectType.Null)).Take(1);

    MetadataReach MetadataEdges(bool contextual)
    {
        var reach = new MetadataReach();
        AddStaticTarget(reach, contextual);
        if (Node.ContainsKey("$dynamicRef")) AddDynamicTargets(reach);
        if (Node.ContainsKey("$recursiveRef")) AddRecursiveTargets(reach);
        return reach;
    }

    void AddStaticTarget(MetadataReach reach, bool contextual)
    {
        if (Node["$ref"] is null) return;
        var reference = ResolveScopedRef(Node["$ref"]!.GetValue<string>());
        if (reference is null) reach.Indeterminate |= contextual;
        else reach.Schemas.Add(reference);
    }

    JsonSchema? InitialContextualTarget(string key, MetadataReach reach)
    {
        var reference = StringValue(Node, key);
        var target = reference is null ? null : ResolveResourceRef(reference);
        reach.Indeterminate |= target is null;
        return target;
    }

    void AddDynamicTargets(MetadataReach reach)
    {
        var target = InitialContextualTarget("$dynamicRef", reach);
        if (target is null) return;
        reach.Schemas.Add(target);
        var reference = StringValue(Node, "$dynamicRef")!;
        var separator = reference.IndexOf('#');
        if (separator < 0) return;
        var fragment = reference[(separator + 1)..];
        var index = Root.ResourceIndex;
        var key = (target.ResourceBase.AbsoluteUri, fragment);
        reach.Indeterminate |= index.IndeterminateResources.Contains(target.ResourceBase.AbsoluteUri);

        // Draft 2020-12 Core 8.2.3.2: only a fragment created by $dynamicAnchor rebinds.
        // A pointer or ordinary $anchor remains a static reference.
        if (!index.DynamicAnchors.TryGetValue(key, out var anchor) || anchor.GetPath() != target.ResolutionScope.Origin) return;
        reach.Indeterminate |= index.Truncated;
        foreach (var resource in ResolutionScope.EnteredResources)
        {
            reach.Indeterminate |= index.IndeterminateResources.Contains(resource.AbsoluteUri);
            if (index.DynamicAnchors.TryGetValue((resource.AbsoluteUri, fragment), out var binding)) reach.Schemas.Add(CreateResolvedSchema(binding));
        }
    }

    void AddRecursiveTargets(MetadataReach reach)
    {
        // Draft 2019-09 Core 8.2.4.2 defines only '#'. Unsupported forms remain indeterminate.
        if (StringValue(Node, "$recursiveRef") != "#")
        {
            reach.Indeterminate = true;
            return;
        }
        var target = InitialContextualTarget("$recursiveRef", reach);
        if (target is null) return;
        reach.Schemas.Add(target);
        if (!RecursiveAnchorIsValid(target.Node)) reach.Indeterminate = true;
        if (!IsRecursiveRoot(target.Node)) return;
        reach.Indeterminate |= Root.ResourceIndex.Truncated;
        AddRecursiveOuterRoots(reach);
    }

    void AddRecursiveOuterRoots(MetadataReach reach)
    {
        foreach (var resource in ResolutionScope.EnteredResources)
        {
            if (!Root.ResourceIndex.Roots.TryGetValue(resource.AbsoluteUri, out var root))
            {
                reach.Indeterminate = true;
                continue;
            }
            reach.Indeterminate |= !RecursiveAnchorIsValid(root);
            if (IsRecursiveRoot(root)) reach.Schemas.Add(CreateResolvedSchema(root));
        }
    }

    /// <summary>
    /// Carries real reachable schema annotations separately from uncertainty; uncertainty never invents a handler.
    /// </summary>
    internal sealed class MetadataReach
    {
        /// <summary>
        /// Gets schemas with concrete, reachable annotations.
        /// </summary>
        internal List<JsonSchema> Schemas { get; } = [];

        /// <summary>
        /// Gets or sets whether some contextual metadata reach could not be proven.
        /// </summary>
        internal bool Indeterminate { get; set; }

        /// <summary>
        /// Gets or sets whether reach includes a context-dependent reference.
        /// </summary>
        internal bool Contextual { get; set; }
    }

    sealed class MetadataTraversal(bool includeChildren)
    {
        readonly MetadataReach _reach = new();
        readonly Queue<MetadataStep> _pending = [];
        readonly HashSet<(JsonObject OriginRoot, string Origin, string Scope, bool Contextual)> _visited = [];

        internal MetadataReach Walk(JsonSchema origin)
        {
            _pending.Enqueue(new(origin, 0, false));
            while (_pending.TryDequeue(out var step)) Visit(step);
            return _reach;
        }

        static bool HasUnexploredContext(MetadataStep step) => step.Contextual || ContainsContextualReference(step.Schema.Node);

        void Visit(MetadataStep step)
        {
            var contextual = step.Contextual || step.Schema.HasContextualReference;
            var scope = step.Schema.ResolutionScope;
            if (!_visited.Add((scope.OriginRoot, scope.Origin, string.Join('\n', scope.EnteredResources), contextual))) return;
            _reach.Contextual |= contextual;
            _reach.Schemas.Add(step.Schema);
            Expand(step with { Contextual = contextual });
        }

        void Expand(MetadataStep step)
        {
            var edges = step.Schema.MetadataEdges(step.Contextual);
            _reach.Indeterminate |= edges.Indeterminate;
            var children = includeChildren ? step.Schema.MetadataChildren(step.Contextual) : [];
            var next = edges.Schemas.Concat(children).ToArray();
            if (AtLimit(step))
            {
                _reach.Indeterminate |= HasUnexploredContext(step) && next.Length > 0;
                return;
            }
            foreach (var child in next) _pending.Enqueue(new(child, step.Depth + 1, step.Contextual));
        }

        bool AtLimit(MetadataStep step) => step.Depth >= MaxResourceTraversalDepth ||
            (step.Contextual && _visited.Count >= MaxMetadataReferenceStates);
    }

    sealed record MetadataStep(JsonSchema Schema, int Depth, bool Contextual);
}
