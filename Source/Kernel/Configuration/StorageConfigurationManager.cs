// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Newtonsoft.Json;

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents an implementation of <see cref="IStorageConfigurationManager"/>.
    /// </summary>
    public class StorageConfigurationManager : IStorageConfigurationManager
    {
        const string StorageFile = "storage.json";

        readonly Storage _config;

        /// <summary>
        /// Initializes a new instance of <see cref="StorageConfigurationManager"/>.
        /// </summary>
        public StorageConfigurationManager()
        {
            var json = File.ReadAllText(StorageFile);
            _config = JsonConvert.DeserializeObject<Storage>(json)!;
        }

        /// <inheritdoc/>
        public object GetForEventStore(Type targetType, TenantId tenantId)
        {
            var configRaw = _config.EventStore.Configuration[tenantId.Value.ToString()].ToString()!;
            if( targetType == typeof(string)) return configRaw;
            return JsonConvert.DeserializeObject(configRaw, targetType)!;
        }
    }
}
