// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.EventSequences.Streaming;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for <see cref="ISiloBuilder"/> for configuring event sequence stream.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    /// Add event sequence stream support.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add for.</param>
    /// <returns><see cref="ISiloBuilder"/> for builder continuation.</returns>
    public static ISiloBuilder AddEventSequenceStreaming(this ISiloBuilder builder)
    {
        builder.AddPersistentStreams(
            WellKnownProviders.EventSequenceStreamProvider,
            EventSequenceQueueAdapterFactory.Create,
            _ =>
            {
                _.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = 8));
                _.ConfigureStreamPubSub();
            });
        return builder;
    }
}
