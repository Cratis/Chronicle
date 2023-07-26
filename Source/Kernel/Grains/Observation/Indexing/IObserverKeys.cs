// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections;

namespace Aksio.Cratis.Kernel.Grains.Observation.Indexing;

/// <summary>
/// Defines an async enumerable for working with keys.
/// </summary>
public interface IObserverKeys : IAsyncEnumerable<Key>
{
}
