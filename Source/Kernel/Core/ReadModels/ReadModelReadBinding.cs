// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the sink and optional explicit occurrence addressed by a materialized read.
/// </summary>
/// <param name="Sink">The sink addressing the read model's physical container.</param>
/// <param name="Occurrence">The explicit occurrence, or null to retain the sink's primary-read semantics.</param>
/// <param name="Visibility">The <see cref="ReadModelInstanceVisibility"/> that decides whether roots stored as uninitialized are read.</param>
public record ReadModelReadBinding(
    ISink Sink,
    ReadModelContainerName? Occurrence,
    ReadModelInstanceVisibility Visibility = ReadModelInstanceVisibility.InitializedOnly)
{
    /// <summary>
    /// Gets a page of instances honoring the binding's visibility.
    /// </summary>
    /// <param name="skip">Number of instances to skip.</param>
    /// <param name="take">Number of instances to take.</param>
    /// <returns>The instances and the total count.</returns>
    /// <remarks>
    /// The sink overload without a visibility is the <see cref="ReadModelInstanceVisibility.InitializedOnly"/> contract,
    /// so it is used for that visibility.
    /// </remarks>
    public Task<ReadModelInstances> GetInstances(int skip, int take) =>
        Visibility == ReadModelInstanceVisibility.InitializedOnly
            ? Sink.GetInstances(Occurrence, skip, take)
            : Sink.GetInstances(Occurrence, skip, take, Visibility);

    /// <summary>
    /// Observes a page of instances honoring the binding's visibility.
    /// </summary>
    /// <param name="skip">Number of instances to skip.</param>
    /// <param name="take">Number of instances to observe.</param>
    /// <returns>An observable stream of instance collections.</returns>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(int skip, int take) =>
        Visibility == ReadModelInstanceVisibility.InitializedOnly
            ? Sink.ObserveInstances(Occurrence, skip, take)
            : Sink.ObserveInstances(Occurrence, skip, take, Visibility);
}
