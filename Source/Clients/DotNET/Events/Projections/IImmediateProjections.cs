// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a system that works with <see cref="IImmediateProjectionFor{TModel}"/>.
/// </summary>
public interface IImmediateProjections
{
    /// <summary>
    /// Get an instance by a specific <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="modelKey"><see cref="ModelKey"/> to get instance for.</param>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <returns>An instance for the id.</returns>
    Task<TModel> GetInstanceById<TModel>(ModelKey modelKey);
}
