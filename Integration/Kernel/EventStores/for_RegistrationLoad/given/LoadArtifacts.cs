// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_RegistrationLoad.given;

/// <summary>
/// The reactor, reducer and projection every simulated client replica registers.
/// </summary>
public static class LoadArtifacts
{
    /// <summary>
    /// The read model the reducer produces.
    /// </summary>
    /// <param name="Name">The customer name.</param>
    public record ReducedCustomer(string Name);

    /// <summary>
    /// The read model the projection produces.
    /// </summary>
    /// <param name="Name">The customer name.</param>
    public record ProjectedCustomer(string Name);

    /// <summary>
    /// The reactor that is registered by every replica.
    /// </summary>
    [DependencyInjection.IgnoreConvention]
    public class LoadReactor : IReactor
    {
        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="context">The event context.</param>
        /// <returns>An awaitable task.</returns>
        public Task Named(CustomerNamed @event, EventContext context) => Task.CompletedTask;
    }

    /// <summary>
    /// The reducer that is registered by every replica.
    /// </summary>
    [DependencyInjection.IgnoreConvention]
    public class LoadReducer : IReducerFor<ReducedCustomer>
    {
        /// <summary>
        /// Reduces the event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="current">The current state.</param>
        /// <returns>The new state.</returns>
        public ReducedCustomer Reduce(CustomerNamed @event, ReducedCustomer? current) => new(@event.Name);
    }

    /// <summary>
    /// The projection that is registered by every replica.
    /// </summary>
    [DependencyInjection.IgnoreConvention]
    public class LoadProjection : IProjectionFor<ProjectedCustomer>
    {
        /// <inheritdoc/>
        public void Define(IProjectionBuilderFor<ProjectedCustomer> builder) => builder
            .From<CustomerNamed>(from => from.Set(model => model.Name).To(@event => @event.Name));
    }
}
