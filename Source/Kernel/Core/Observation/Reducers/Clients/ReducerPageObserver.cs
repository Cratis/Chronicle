// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Delegate that gets called when a page of partitions spanning event sources is to be reduced.
/// </summary>
/// <param name="operations">The <see cref="ReduceOperation"/> of each partition in the page.</param>
/// <param name="completions">The <see cref="TaskCompletionSource{T}"/> of each partition to return its <see cref="ReducerSubscriberResult"/> to.</param>
public delegate void ReducerPageObserver(IReadOnlyList<ReduceOperation> operations, IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> completions);
