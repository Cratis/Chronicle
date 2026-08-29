// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines the subscriber that feeds observed events into pattern mining.
/// </summary>
/// <remarks>
/// Unpartitioned on purpose. The sketch behind mining is per scope rather than per event source, and it is guarded
/// by a process-local lock, so spreading this across silos would have two of them counting into the same scope
/// without seeing each other.
/// </remarks>
public interface IPatternCaptureSubscriber : IObserverSubscriber, IUnpartitionedObserverSubscriber;
