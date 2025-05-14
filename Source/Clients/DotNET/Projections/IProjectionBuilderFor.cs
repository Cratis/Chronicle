// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building out a <see cref="IProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public interface IProjectionBuilderFor<TModel> : IProjectionBuilder<TModel, IProjectionBuilderFor<TModel>>
{
    /// <summary>
    /// Specifies the <see cref="EventSequenceId"/> to use as source.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to use.</param>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TModel> FromEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Names the model - typically used by storage as name of storage unit (collection, table etc.)
    /// </summary>
    /// <param name="modelName">Name of the model.</param>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TModel> ModelName(string modelName);

    /// <summary>
    /// Set the projection to not be rewindable - its a moving forward only projection.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TModel> NotRewindable();

    /// <summary>
    /// Set the projection not be active, meaning that it won't actively observe.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilderFor<TModel> Passive();
}
