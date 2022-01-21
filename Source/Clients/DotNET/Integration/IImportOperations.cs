// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Defines import operations that can be performed.
    /// </summary>
    /// <typeparam name="TModel">Type of model the operations are for.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model the operations are for.</typeparam>
    public interface IImportOperations<TModel, TExternalModel> : IDisposable
    {
        /// <summary>
        /// Gets the adapter for the operations.
        /// </summary>
        IAdapterFor<TModel, TExternalModel> Adapter { get; }

        /// <summary>
        /// Gets the projection used for the operations.
        /// </summary>
        IAdapterProjectionFor<TModel> Projection { get; }

        /// <summary>
        /// Gets the mapper used for the operations.
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// Apply an instance of the external model.
        /// </summary>
        /// <param name="instance">The external model instance.</param>
        /// <remarks>
        /// Objects will be mapped to the model and compared for changes and then run through
        /// the translation of changes to events.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Apply(TExternalModel instance);

        /// <summary>
        /// Apply instances of the external model.
        /// </summary>
        /// <param name="instances">The external model instances.</param>
        /// <remarks>
        /// Objects will be mapped to the model and compared for changes and then run through
        /// the translation of changes to events.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Apply(IEnumerable<TExternalModel> instances);
    }
}
