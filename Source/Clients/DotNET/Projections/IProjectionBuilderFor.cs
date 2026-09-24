// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building out a <see cref="IProjectionFor{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
public interface IProjectionBuilderFor<TReadModel> : IProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>
{
    /// <summary>
    /// Specifies the <see cref="EventSequenceId"/> to use as source.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to use.</param>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TReadModel> FromEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Names the model container - typically used by storage as name of storage unit (collection, table, etc.).
    /// </summary>
    /// <param name="containerName">Container name of the read model.</param>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TReadModel> ContainerName(string containerName);

    /// <summary>
    /// Set the projection to not be rewindable - its a moving forward only projection.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TReadModel> NotRewindable();

    /// <summary>
    /// Set the projection not be active, meaning that it won't actively observe.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TReadModel> Passive();

    /// <summary>
    /// Declares this projection as one of several mutually exclusive representations of the same logical entity.
    /// </summary>
    /// <typeparam name="TIdentity">The type anchoring the logical identity the variants share. It needs no read model and no common base type with the variants.</typeparam>
    /// <param name="keyAccessor">Accessor for this read model's own key, used to correlate an event back to an already-active instance.</param>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// Every projection declaring the same <typeparamref name="TIdentity"/> forms a group. Entering one variant
    /// removes the entity from every other variant in that group. Only the events named with
    /// <see cref="EntersOn{TEvent}"/> may create this variant; every other event it projects from becomes an
    /// update-only mapping that can bring an active instance up to date but never create or resurrect one.
    /// </remarks>
    IProjectionBuilderFor<TReadModel> VariantOf<TIdentity>(Expression<Func<TReadModel, object?>> keyAccessor);

    /// <summary>
    /// Declares the event that activates this variant.
    /// </summary>
    /// <typeparam name="TEvent">Type of event that activates this variant.</typeparam>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// Only for a projection that also declares <see cref="VariantOf{TIdentity}"/>. The event keeps its ordinary
    /// create-or-update behavior; mapping its properties is still done with the usual
    /// <c language="csharp">From</c> call.
    /// </remarks>
    IProjectionBuilderFor<TReadModel> EntersOn<TEvent>();
}
