// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a system that works with <see cref="IImmediateProjectionFor{TModel}"/>.
/// </summary>
public interface IImmediateProjections
{
    /// <summary>
    /// Get an instance by a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="IImmediateProjectionFor{TModel}"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get instance for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>An instance for the id.</returns>
    Task<TModel> GetInstanceById<TModel>(IImmediateProjectionFor<TModel> projectionDefinition, EventSourceId eventSourceId);
}
