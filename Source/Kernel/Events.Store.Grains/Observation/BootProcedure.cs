// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Boot;
using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents a <see cref="IPerformBootProcedure"/> for the event store.
    /// </summary>
    public class BootProcedure : IPerformBootProcedure
    {
        readonly IGrainFactory _grainFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootProcedure"/> class.
        /// </summary>
        /// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
        public BootProcedure(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            var observers = _grainFactory.GetGrain<IObservers>(Guid.Empty);
            observers.RetryFailed();
        }
    }
}
