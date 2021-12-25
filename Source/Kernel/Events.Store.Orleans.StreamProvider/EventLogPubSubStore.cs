// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

namespace Cratis.Events.Store.Orleans.StreamProvider
{
    public class EventLogPubSubStore : IGrainStorage
    {
        readonly JsonSerializerSettings _serializerSettings;

        public EventLogPubSubStore(ITypeResolver typeResolver, IGrainFactory grainFactory)
        {
            _serializerSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, grainFactory);
        }

        /// <inheritdoc/>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (File.Exists("pubsub.json"))
            {
                var json = File.ReadAllText("pubsub.json");
                grainState.State = JsonConvert.DeserializeObject(json, grainState.Type, _serializerSettings);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var json = JsonConvert.SerializeObject(grainState.State, _serializerSettings);
            File.WriteAllText("pubsub.json", json);
            return Task.CompletedTask;
        }
    }
}
