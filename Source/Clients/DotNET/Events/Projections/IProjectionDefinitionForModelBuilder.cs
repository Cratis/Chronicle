// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the builder for building out projection related to a model.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    public interface IProjectionDefinitionForModelBuilder<TModel>
    {
        /// <summary>
        /// Start building the from expressions for a specific event type.
        /// </summary>
        /// <param name="builderCallback">Callback for building</param>
        /// <typeparam name="TEvent">Type of event.</typeparam>
        /// <returns>Builder continuation."</returns>
        IProjectionDefinitionForModelBuilder<TModel> From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback);
    }
}
