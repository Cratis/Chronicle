// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents a <see cref="Module"/> for dealing with storage configuration.
    /// </summary>
    public class StorageModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            var storageConfigurationManager = new StorageConfigurationManager();
            builder.RegisterSource(new EventStoreConfigurationAsRegistrationSource(storageConfigurationManager));
        }
    }
}
