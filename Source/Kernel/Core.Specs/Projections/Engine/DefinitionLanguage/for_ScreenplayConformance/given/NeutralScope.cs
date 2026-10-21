// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One projection level: what a projection, a child collection or a nested object declares.
/// </summary>
/// <param name="From">The transitions.</param>
/// <param name="Joins">The joined events.</param>
/// <param name="Children">The child collections.</param>
/// <param name="Nested">The nested objects.</param>
/// <param name="Every">The <c language="csharp">every</c> or <c language="csharp">all</c> mappings, or null when the level has none.</param>
/// <param name="Removals">The removing events.</param>
/// <param name="JoinRemovals">The removing joined events.</param>
public record NeutralScope(
    IReadOnlyList<NeutralFrom> From,
    IReadOnlyList<NeutralJoin> Joins,
    IReadOnlyList<NeutralChildren> Children,
    IReadOnlyList<NeutralNested> Nested,
    NeutralEvery? Every,
    IReadOnlyList<NeutralRemoval> Removals,
    IReadOnlyList<NeutralJoinRemoval> JoinRemovals)
{
    /// <summary>
    /// Renders the scope canonically: every collection sorted, so two scopes are equivalent exactly when their renderings are equal.
    /// </summary>
    /// <returns>The canonical text.</returns>
    public string Render()
    {
        var builder = new StringBuilder();
        Render(builder, 0);
        return builder.ToString();
    }

    static string Indent(int depth) => new(' ', depth * 2);

    static void Render(StringBuilder builder, IEnumerable<NeutralMapping> mappings, int depth)
    {
        foreach (var mapping in mappings.OrderBy(_ => _.Target, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}{mapping.Target} {mapping.Operation}{(mapping.Source is null ? string.Empty : $" {mapping.Source}")}");
        }
    }

    void Render(StringBuilder builder, int depth)
    {
        foreach (var from in From.OrderBy(_ => _.Event, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}from {from.Event} key {from.Key}{(from.ParentKey is null ? string.Empty : $" parent {from.ParentKey}")}");
            Render(builder, from.Mappings, depth + 1);
        }

        foreach (var join in Joins.OrderBy(_ => _.Event, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}join {join.Event} on {join.On}{(join.Key is null ? string.Empty : $" key {join.Key}")}");
            Render(builder, join.Mappings, depth + 1);
        }

        if (Every is not null)
        {
            builder.AppendLine($"{Indent(depth)}every includeChildren={Every.IncludeChildren} subscribesToAllEvents={Every.SubscribesToAllEvents}");
            Render(builder, Every.Mappings, depth + 1);
        }

        foreach (var removal in Removals.OrderBy(_ => _.Event, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}remove with {removal.Event} key {removal.Key}{(removal.ParentKey is null ? string.Empty : $" parent {removal.ParentKey}")}");
        }

        foreach (var removal in JoinRemovals.OrderBy(_ => _.Event, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}remove via join {removal.Event} key {removal.Key}");
        }

        foreach (var children in Children.OrderBy(_ => _.Property, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}children {children.Property} identified by {children.IdentifiedBy}");
            children.Scope.Render(builder, depth + 1);
        }

        foreach (var nested in Nested.OrderBy(_ => _.Property, StringComparer.Ordinal))
        {
            builder.AppendLine($"{Indent(depth)}nested {nested.Property}");
            nested.Scope.Render(builder, depth + 1);
        }
    }
}
