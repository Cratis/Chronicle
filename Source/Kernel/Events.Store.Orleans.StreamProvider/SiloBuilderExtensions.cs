// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Store.Orleans.Streams;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Streams;

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
            // var configurator = new EventLogSiloPersistentStreamConfigurator(
            //     "event-log",
            //     (_) => { });
            // builder.ConfigureApplicationParts(_ => _.AddFrameworkPart(typeof(EventLogSiloPersistentStreamConfigurator).Assembly));

            // configurator.UseDynamicClusterConfigDeploymentBalancer();
            // configurator.ConfigureStreamPubSub(StreamPubSubType.ExplicitGrainBasedAndImplicit);
            // configurator.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = 8));

            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ =>
                {
                    _.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = 8));
                    _.ConfigureStreamPubSub();
                    _.UseDynamicClusterConfigDeploymentBalancer();
                });
            return builder;
        }
    }


    public class EventLogSiloPersistentStreamConfigurator : SiloPersistentStreamConfigurator
    {
        public EventLogSiloPersistentStreamConfigurator(string name, Action<Action<IServiceCollection>> configureDelegate) : base(name, configureDelegate, EventLogQueueAdapterFactory.Create)
        {
            this.ConfigureComponent(EventLogQueueAdapterFactory.Create);
        }
    }
}
