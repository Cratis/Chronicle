// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Represents an implementation of <see cref="IAdapterProjectionFactory"/>.
    /// </summary>
    public class AdapterProjectionFactory : IAdapterProjectionFactory
    {
        readonly IEventTypes _eventTypes;
        readonly IJsonSchemaGenerator _schemaGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterProjectionFactory"/> class.
        /// </summary>
        /// <param name="eventTypes">The <see cref="IEventTypes"/> to use.</param>
        /// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for generating schemas.</param>
        public AdapterProjectionFactory(
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator)
        {
            _eventTypes = eventTypes;
            _schemaGenerator = schemaGenerator;
        }

        /// <inheritdoc/>
        public IAdapterProjectionFor<TModel> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
        {
            var projectionBuilder = new ProjectionBuilderFor<TModel>(Guid.Empty, _eventTypes, _schemaGenerator);
            adapter.DefineModel(projectionBuilder);

            return new AdapterProjectionFor<TModel>();
        }
    }
}
