// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Identifies the child projections that target the same read model type as their parent, e.g.
/// <c language="csharp">Feature.SubFeatures</c> where a feature holds features. A node of such a type can live at any
/// depth, so a child of that type can have its parent created by the recursive child's events too.
/// </summary>
internal static class RecursiveChildProjections
{
    /// <summary>
    /// Gets the child projections of <paramref name="projection"/> that target the same read model type.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> to inspect.</param>
    /// <returns>The recursive child projections.</returns>
    /// <remarks>
    /// A recursive collection item is emitted as a <c language="json">$ref</c> to its first occurrence, which does not resolve to the
    /// same schema instance. Compare available schema identities, then the title and property names, types and formats,
    /// including array item titles. Without an identity, structurally identical schemas cannot be distinguished.
    /// </remarks>
    internal static IEnumerable<IProjection> Of(IProjection projection) =>
        projection.ChildProjections.Where(child => IsSameType(child.TargetReadModelSchema, projection.TargetReadModelSchema));

    /// <summary>
    /// Gets the projection that owns an event type: <paramref name="projection"/> itself, or one of its recursive children.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> a parent event was found for.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> of the parent event.</param>
    /// <returns>The <see cref="IProjection"/> whose key resolver places the node the event created.</returns>
    internal static IProjection OwnerOf(IProjection projection, EventTypeId eventTypeId) =>
        projection.OwnEventTypes.Any(type => type.Id == eventTypeId)
            ? projection
            : Of(projection).FirstOrDefault(child => child.OwnEventTypes.Any(type => type.Id == eventTypeId)) ?? projection;

    static bool IsSameType(JsonSchema? child, JsonSchema? parent)
    {
        if (child is null || parent is null)
        {
            return false;
        }

        var childSchema = child.ActualSchema;
        var parentSchema = parent.ActualSchema;
        if (string.IsNullOrEmpty(parentSchema.Title))
        {
            return false;
        }
        if (childSchema.Title != parentSchema.Title || !HasSameIdentity(child, parent))
        {
            return false;
        }

        return HasSameProperties(childSchema, parentSchema);
    }

    static bool HasSameProperties(JsonSchema child, JsonSchema parent)
    {
        var childProperties = child.ActualProperties;
        var parentProperties = parent.ActualProperties;
        return childProperties.Count == parentProperties.Count && parentProperties.All(property =>
            childProperties.TryGetValue(property.Key, out var childProperty) && HasSamePropertyType(childProperty, property.Value));
    }

    static bool HasSamePropertyType(JsonSchema child, JsonSchema parent)
    {
        var childSchema = child.ActualSchema;
        var parentSchema = parent.ActualSchema;
        if ((childSchema.Type, childSchema.Format) != (parentSchema.Type, parentSchema.Format))
        {
            return false;
        }
        if (!HasSameIdentity(child, parent))
        {
            return false;
        }

        return HasSameItems(childSchema.Item, parentSchema.Item);
    }

    static bool HasSameItems(JsonSchema? child, JsonSchema? parent)
    {
        if (child is null || parent is null)
        {
            return child is null && parent is null;
        }

        var childSchema = child.ActualSchema;
        var parentSchema = parent.ActualSchema;
        return (childSchema.Type, childSchema.Format, childSchema.Title) == (parentSchema.Type, parentSchema.Format, parentSchema.Title)
            && HasSameIdentity(child, parent);
    }

    static bool HasSameIdentity(JsonSchema child, JsonSchema parent)
    {
        var childId = child.ActualSchema.Node["$id"]?.GetValue<string>();
        var parentId = parent.ActualSchema.Node["$id"]?.GetValue<string>();
        if (childId is not null || parentId is not null)
        {
            return IdentifiersMatch(child, childId, parent, parentId);
        }

        return HasSameReference(child, parent);
    }

    static bool HasSameReference(JsonSchema child, JsonSchema parent)
    {
        var childReference = child.Node["$ref"]?.GetValue<string>();
        var parentReference = parent.Node["$ref"]?.GetValue<string>();
        if (childReference is null && parentReference is null)
        {
            return true;
        }
        if (childReference is not null && parentReference is not null)
        {
            return IdentifiersMatch(child, childReference, parent, parentReference);
        }

        // A resolved reference is a clone; compare its target in the same document, not object identity.
        return ReferenceEquals(child.Root, parent.Root) && JsonNode.DeepEquals(child.ActualSchema.Node, parent.ActualSchema.Node);
    }

    static bool IdentifiersMatch(JsonSchema child, string? childId, JsonSchema parent, string? parentId)
    {
        if (childId is null || parentId is null)
        {
            return false;
        }

        var childUri = AbsoluteIdentifier(child, childId);
        var parentUri = AbsoluteIdentifier(parent, parentId);
        if (childUri is not null || parentUri is not null)
        {
            return childUri == parentUri;
        }

        return ReferenceEquals(child.Root, parent.Root) && childId == parentId;
    }

    static Uri? AbsoluteIdentifier(JsonSchema schema, string identifier)
    {
        if (Uri.TryCreate(identifier, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        var rootId = schema.Root.Node["$id"]?.GetValue<string>();
        return Uri.TryCreate(rootId, UriKind.Absolute, out var rootUri) && Uri.TryCreate(rootUri, identifier, out var resolved)
            ? resolved
            : null;
    }
}
