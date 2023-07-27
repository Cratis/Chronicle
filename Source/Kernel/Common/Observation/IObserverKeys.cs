// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Defines an async enumerable for working with keys.
/// </summary>
public interface IObserverKeys : IAsyncEnumerable<Key>
{
}
