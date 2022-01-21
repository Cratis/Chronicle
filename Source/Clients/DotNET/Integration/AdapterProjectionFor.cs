// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Newtonsoft.Json;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
    {
        /// <inheritdoc/>
        public TModel GetById(EventSourceId eventSourceId)
        {
            // var eventProvider = new EventSourceInstanceEventProvider(_eventStream, eventSourceId);
            // var pipeline = new ProjectionPipeline(eventProvider, _projection, _changesetStorage, _loggerFactory.CreateLogger<ProjectionPipeline>());
            // var result = new InstanceProjectionResult<TModel>();
            // pipeline.StoreIn(result);
            // pipeline.Start();

            // if (result.HasInstance(eventSourceId)) return result.GetInstance(eventSourceId);
            return JsonConvert.DeserializeObject<TModel>("{}")!;
        }
    }
}
