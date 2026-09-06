// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines the subscriber that feeds observed events into pattern mining.
/// </summary>
/// <remarks>
/// Unpartitioned on purpose. Every batch it observes becomes one call to the <see cref="IPatternMiner"/> of its
/// event store and namespace - itself a single activation - so partitioning the subscriber per event source would
/// only fan thousands of activations into that one grain with smaller, more frequent calls. One activation keeps
/// the batches coalesced per store.
/// </remarks>
public interface IPatternCaptureSubscriber : IObserverSubscriber, IUnpartitionedObserverSubscriber;
