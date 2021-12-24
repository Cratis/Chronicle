// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Orleans.StreamProvider;
using Orleans.Configuration;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="ISiloBuilder"/> for configuring event log stream.
    /// </summary>
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Add event log stream support.
        /// </summary>
        /// <param name="builder"><see cref="ISiloBuilder"/> to add for.</param>
        /// <returns><see cref="ISiloBuilder"/> for builder continuation.</returns>
        public static ISiloBuilder AddEventLogStream(this ISiloBuilder builder)
        {
            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ =>
                {
                    _.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = 8));
                    _.ConfigureStreamPubSub();
                    // _.UseDynamicClusterConfigDeploymentBalancer();
                });
            return builder;
        }
    }
}
