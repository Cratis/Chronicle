// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used on a type decorated with <see cref="VariantOfAttribute{TIdentity}"/> to declare the event that
/// activates (creates) this variant.
/// </summary>
/// <remarks>
/// Only the event(s) declared with this attribute may create or resurrect the variant. Every other event handled
/// by the variant - whether declared directly on it or shared through a global handler - only updates an
/// already-active instance of this variant and can never create one, so it can never resurrect the entity into a
/// variant it has since left.
/// </remarks>
/// <typeparam name="TEvent">The type of event that activates this variant.</typeparam>
/// <param name="key">Optional property name on the event that identifies the read model instance. Defaults to using the event source identifier.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EntersOnAttribute<TEvent>(string? key = default) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the type of event that activates this variant.
    /// </summary>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the property name on the event that identifies the read model instance.
    /// </summary>
    public string? Key { get; } = key;
}
