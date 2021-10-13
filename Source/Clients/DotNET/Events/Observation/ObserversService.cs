// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Represents a <see cref="IHostedService"/> for working with observers.
    /// </summary>
    public class ObserversService : IHostedService
    {
        readonly IObservers _observers;

        public ObserversService(IObservers observers)
        {
            _observers = observers;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _observers.StartObserving();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
