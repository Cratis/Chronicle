// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Orleans.StreamProvider;
using Orleans.Hosting;

namespace Orleans
{
    public static class ClientBuilderExtensions
    {
        public static IClientBuilder AddEventLogStream(this IClientBuilder builder)
        {
            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ => {});
            return builder;
        }
    }
}
