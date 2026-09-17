// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to declare event handlers once for every variant of <typeparamref name="TIdentity"/>, rather
/// than repeating them on each <see cref="VariantOfAttribute{TIdentity}"/> type.
/// </summary>
/// <remarks>
/// The decorated type carries the shared mappings as ordinary model-bound members - for example a property with
/// <see cref="SetFromAttribute{TEvent}"/>. Every mapping is merged into every variant of
/// <typeparamref name="TIdentity"/>, and every variant must have the members the mappings target; one that does
/// not is a declaration error rather than a silently skipped mapping.
/// <para>
/// A shared handler can only ever update an already-active variant. It is merged as an update-only,
/// self-referential join, so an event handled globally never creates a variant and can never resurrect the
/// entity into a variant it has since left.
/// </para>
/// </remarks>
/// <typeparam name="TIdentity">The type that anchors the logical identity the variants are grouped under.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class GlobalForAttribute<TIdentity> : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the type that anchors the logical identity the variants are grouped under.
    /// </summary>
    public Type Identity => typeof(TIdentity);
}
