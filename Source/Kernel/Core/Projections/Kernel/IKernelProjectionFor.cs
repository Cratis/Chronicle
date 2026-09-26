// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Defines a projection the kernel declares for itself, rather than one a client registers.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
/// <remarks>
/// <para>
/// Chronicle's own operational figures - how many events an event store holds, how they are spread across event
/// types, how many observers are failing - are the same shape of question a projection answers for an application.
/// Computing them by querying storage on every request re-reads the whole store to produce a number that only
/// changes when an event is appended, so they are projected instead.
/// </para>
/// <para>
/// A system projection is discovered by type rather than registered over the wire, is owned by
/// <see cref="Concepts.Projections.ProjectionOwner.Kernel"/>, carries the "$system." identifier prefix kernel
/// reactors and pattern capture already use, and is never replayable.
/// </para>
/// </remarks>
public interface IKernelProjectionFor<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Define the projection.
    /// </summary>
    /// <param name="builder"><see cref="IKernelProjectionBuilder{TReadModel}"/> to build with.</param>
    void Define(IKernelProjectionBuilder<TReadModel> builder);
}
