// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a read model is one of several mutually exclusive representations of the same
/// logical entity. Every type marked with this attribute for the same <typeparamref name="TIdentity"/> forms a
/// group: entering one variant (see <see cref="EntersOnAttribute{TEvent}"/>) removes the entity from every other
/// variant in the group.
/// </summary>
/// <typeparam name="TIdentity">
/// The type that anchors the logical identity shared by every variant in the group. It does not need to be a
/// read model itself, and it does not need a common CLR base type with any of the variants.
/// </typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class VariantOfAttribute<TIdentity> : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the type that anchors the logical identity shared by every variant in the group.
    /// </summary>
    public Type Identity => typeof(TIdentity);
}
