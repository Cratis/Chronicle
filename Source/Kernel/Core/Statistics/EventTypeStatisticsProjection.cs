// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Kernel;

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Declares the projection that counts events per event type per namespace.
/// </summary>
/// <remarks>
/// <para>
/// Materialized per namespace. Store-wide queries concatenate these rows rather than maintaining a shared
/// global sink, whose rebuild would require coordinated ownership across all contributing namespaces.
/// </para>
/// <para>
/// The key is composite rather than the event source id, which is what makes this a projection at all: counting by
/// event type is counting by something that is not the stream the event belongs to. Every event in the store folds
/// into the row for its (event type, namespace) pair, so the figures cost one append each rather than a scan of the
/// whole store per request.
/// </para>
/// </remarks>
[KernelProjection("statistics.event-types")]
public class EventTypeStatisticsProjection : IKernelProjectionFor<EventTypeStatistics>
{
    /// <inheritdoc/>
    public void Define(IKernelProjectionBuilder<EventTypeStatistics> builder) => builder
        .IdentifiedByComposite(key => key
            .With(_ => _.EventType, "$eventContext(EventType.Id)")
            .With(_ => _.Namespace, "$eventContext(Namespace)"))
        .FromEvery(from => from.Count(_ => _.Count));
}
