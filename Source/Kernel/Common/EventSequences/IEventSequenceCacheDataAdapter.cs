// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Defines a data adapter for working with <see cref="CachedMessage"/> and <see cref="IBatchContainer"/>.
/// </summary>
public interface IEventSequenceCacheDataAdapter : ICacheDataAdapter
{
    /// <summary>
    /// Gets the <see cref="IBatchContainer"/> from <see cref="CachedMessage"/>.
    /// </summary>
    /// <param name="batchContainer"><see cref="IBatchContainer"/> to get for.</param>
    /// <returns>A <see cref="CachedMessage"/>.</returns>
    CachedMessage GetCachedMessage(IBatchContainer batchContainer);
}
