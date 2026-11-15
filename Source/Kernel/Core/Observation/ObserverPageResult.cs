// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the outcome of delivering a page that spans partitions: one <see cref="ObserverSubscriberResult"/> per partition,
/// so a partition that failed does not take the others in the page with it.
/// </summary>
/// <param name="Results">The result of each partition in the page.</param>
public record ObserverPageResult(IReadOnlyDictionary<Key, ObserverSubscriberResult> Results);
