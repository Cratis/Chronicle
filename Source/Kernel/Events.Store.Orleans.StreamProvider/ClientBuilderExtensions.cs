// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Store.Orleans.Streams;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Orleans
{
    public static class ClientBuilderExtensions
    {
        public static IClientBuilder AddEventLogStream(this IClientBuilder builder)
        {
            builder.ConfigureServices(_ => _.AddSingleton<EventLogQueueAdapterFactory>());

            var configurator = new ClusterClientPersistentStreamConfigurator(
                EventLogQueueAdapter.StreamName,
                builder,
                (sp, name) => sp.GetService<EventLogQueueAdapterFactory>());
            return builder;
        }
    }
}
