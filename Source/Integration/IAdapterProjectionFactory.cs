// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Defines a factory for creating <see cref="IAdapterProjectionFor{TModel}"/>.
    /// </summary>
    public interface IAdapterProjectionFactory
    {
        /// <summary>
        /// Create a <see cref="IAdapterProjectionFor{TModel}"/> from an <see cref="IAdapterFor{TModel, TExternalModel}"/>.
        /// </summary>
        /// <param name="adapter"><see cref="IAdapterFor{TModel, TExternalModel}"/> to create for.</param>
        /// <typeparam name="TModel">Type of model.</typeparam>
        /// <typeparam name="TExternalModel">Type of external model.</typeparam>
        /// <returns>A new instance of <see cref="IAdapterProjectionFor{TModel}"/>.</returns>
        IAdapterProjectionFor<TModel> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter);
    }
}
