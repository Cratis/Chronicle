// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Grains;
using Cratis.Events.Store.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Orleans
{
    public static class ClientBuilderExtensions
    {
        public static IClientBuilder AddEventLogStream(this IClientBuilder builder)
        {
            builder.ConfigureServices(services => services
                .AddSingleton<IEventStoreDatabase, EventStoreDatabase>()
                .AddSingleton(sp =>
                {
                    var grain = sp
                        .GetService<IClusterClient>()!
                        .GetGrain<IEventStoreConfiguration>(Guid.Empty);

                    var task = grain.Get();
                    task.Wait();
                    return task.Result;
                }));

            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ => { });
            return builder;
        }
    }
}
