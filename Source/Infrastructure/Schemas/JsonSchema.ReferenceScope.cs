// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchema
{
    /// <summary>
    /// Gets whether this node has a context-dependent reference, including an invalid reference value.
    /// </summary>
    internal bool HasContextualReference => Node.ContainsKey("$dynamicRef") || Node.ContainsKey("$recursiveRef");

    bool HasReferenceKeyword => Node["$ref"] is not null;

    string? ReferenceValue => Node["$ref"]?.GetValue<string>();

    ResolutionContext ResolutionScope => _resolutionContext ?? new(ResourceBase, Node.GetPath(), [ResourceBase], Node);

    static ResolutionContext? ScopeFromParent(JsonSchema? root, JsonObject node) => root?.ChildResolutionContext(node);

    static bool IsChildOf(JsonObject node, JsonObject parent)
    {
        for (var ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, parent)) return true;
        }
        return false;
    }

    static string? StringValue(JsonObject node, string key) =>
        node[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static bool IsRecursiveRoot(JsonObject node) =>
        node["$recursiveAnchor"] is JsonValue value && value.TryGetValue<bool>(out var recursive) && recursive;

    static void IndexDynamicAnchor(JsonObject node, Uri resource, ResourceIndexContext index)
    {
        if (!node.ContainsKey("$dynamicAnchor")) return;
        var anchor = StringValue(node, "$dynamicAnchor");
        if (anchor is null || !IsAnchorName(anchor))
        {
            index.IndeterminateResources.Add(resource.AbsoluteUri);
            return;
        }
        if (!index.DynamicGroups.TryGetValue(anchor, out var group)) index.DynamicGroups[anchor] = group = [];
        group.Add(node);
        var key = (resource.AbsoluteUri, anchor);
        if (!index.DynamicAnchors.TryAdd(key, node)) index.IndeterminateResources.Add(resource.AbsoluteUri);
        if (index.Anchors.TryGetValue(key, out var existing) && !ReferenceEquals(existing, node)) index.IndeterminateResources.Add(resource.AbsoluteUri);
        index.Anchors.TryAdd(key, node);
    }

    static bool IsAnchorName(string anchor) =>
        anchor.Length > 0 && IsAnchorStart(anchor[0]) && anchor.All(IsAnchorCharacter);

    static bool IsAnchorStart(char character) => char.IsAsciiLetter(character) || character == '_';

    static bool IsAnchorCharacter(char character) =>
        IsAnchorStart(character) || char.IsAsciiDigit(character) || character is '-' or '.';

    ResolutionContext ChildResolutionContext(JsonObject node)
    {
        var isChild = IsChildOf(node, Node);
        var origin = isChild ? ResolutionScope.Origin + node.GetPath()[Node.GetPath().Length..] : node.GetPath();
        var originRoot = isChild ? ResolutionScope.OriginRoot : node;
        return EnterResource(ResourceId(node, ResourceBase), origin, originRoot);
    }

    ResolutionContext EnterResource(Uri resource, string origin, JsonObject? originRoot = null)
    {
        // Operational resolution retains its entered resources; security classification ignores routes.
        var entered = ResolutionScope.EnteredResources;
        return new(resource, origin, entered.Contains(resource) ? entered : [.. entered, resource], originRoot ?? ResolutionScope.OriginRoot);
    }

    /// <summary>
    /// Identifies the original schema location and the resources entered on the route to a wrapper.
    /// </summary>
    /// <param name="ResourceBase">The effective base URI.</param>
    /// <param name="Origin">The original document location, unchanged by deep cloning.</param>
    /// <param name="EnteredResources">The ordered, distinct entered resources.</param>
    /// <param name="OriginRoot">The original JSON root whose reference identity distinguishes separate schema trees.</param>
    internal sealed record ResolutionContext(Uri ResourceBase, string Origin, IReadOnlyList<Uri> EnteredResources, JsonObject OriginRoot);
}
