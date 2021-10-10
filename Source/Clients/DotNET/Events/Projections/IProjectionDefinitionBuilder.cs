// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the builder for building out a <see cref="IProjectionDefinitionBuilder"/>.
    /// </summary>
    public interface IProjectionDefinitionBuilder
    {
        /// <summary>
        /// Associates the target model.
        /// </summary>
        /// <param name="modelName">Optional name of the model - typically used by storage as name of storage unit (collection, table etc.)</param>
        /// <typeparam name="TModel">Type of model.</typeparam>
        /// <returns>Model Builder."</returns>
        IProjectionDefinitionForModelBuilder<TModel> Model<TModel>(string? modelName = default);
    }
}
