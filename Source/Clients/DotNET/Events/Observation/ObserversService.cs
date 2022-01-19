// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Aksio.Cratis.Events.Observation
{
    /// <summary>
    /// Represents a <see cref="IHostedService"/> for working with observers.
    /// </summary>
    public class ObserversService : IHostedService
    {
        readonly IObservers _observers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObserversService"/> class.
        /// </summary>
        /// <param name="observers"><see cref="IObservers"/> to work with.</param>
        public ObserversService(IObservers observers)
        {
            _observers = observers;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _observers.StartObserving();
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
