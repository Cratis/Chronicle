// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Store.Orleans.Streams;
using Orleans.Hosting;
using Orleans.Streams;

namespace Orleans
{
    public static class ClientBuilderExtensions
    {
        public static IClientBuilder AddEventLogStream(this IClientBuilder builder)
        {
            // var configurator = new EventLogClusterClientPersistentStreamConfigurator(
            //     "event-log",
            //     builder);

            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ =>
                {
                });
            return builder;
        }
    }


    public class EventLogClusterClientPersistentStreamConfigurator : ClusterClientPersistentStreamConfigurator
    {
        public EventLogClusterClientPersistentStreamConfigurator(string name, IClientBuilder clientBuilder) : base(name, clientBuilder, EventLogQueueAdapterFactory.Create)
        {
            this.ConfigureComponent(EventLogQueueAdapterFactory.Create);
        }
    }
}
