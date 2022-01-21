// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using AutoMapper;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Defines an adapter that acts as a translation layer between data and an event sourced model.
    /// </summary>
    /// <typeparam name="TModel">Model the adapter is for.</typeparam>
    /// <typeparam name="TExternalModel">Type in the external system.</typeparam>
    public interface IAdapterFor<TModel, TExternalModel>
    {
        /// <summary>
        /// Gets the unique identifier for the adapter.
        /// </summary>
        /// <value></value>
        AdapterId Identifier { get; }

        /// <summary>
        /// Gets the property that represents the key.
        /// </summary>
        Func<TExternalModel, EventSourceId> KeyResolver { get; }

        /// <summary>
        /// Define the model / state based on projection from events.
        /// </summary>
        /// <param name="builder"><see cref="IProjectionBuilderFor{TModel}"/>.</param>
        void DefineModel(IProjectionBuilderFor<TModel> builder);

        /// <summary>
        /// Define the translation of incoming data to zero or more events.
        /// </summary>
        /// <param name="builder"><see cref="IImportBuilderFor{TModel, TExternalModel}"/> for building what to do when input changes occur.</param>
        void DefineImport(IImportBuilderFor<TModel, TExternalModel> builder);

        /// <summary>
        /// Define the mapping between an input model and the model used in the adapter.
        /// </summary>
        /// <param name="builder">The <see cref="IMappingExpression{TExternalSystem, TModel}"/> to use.</param>
        void DefineImportMapping(IMappingExpression<TExternalModel, TModel> builder);
    }
}
