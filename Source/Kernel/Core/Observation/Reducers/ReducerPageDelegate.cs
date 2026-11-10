// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents the reducer delegate for a page of partitions, reduced in one delivery.
/// </summary>
/// <param name="partitions">The <see cref="ReducerPagePartition"/> of every partition in the page.</param>
/// <returns>The <see cref="ReducerSubscriberResult"/> of each partition that was answered for.</returns>
public delegate Task<IReadOnlyDictionary<Key, ReducerSubscriberResult>> ReducerPageDelegate(IReadOnlyList<ReducerPagePartition> partitions);
