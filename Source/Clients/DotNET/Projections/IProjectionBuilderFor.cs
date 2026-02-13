// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
