// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Grpc;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="Observers"/>.
    /// </summary>
    public class Observers : IObservers
    {
        readonly IEnumerable<IObserver> _observers;

        /// <summary>
        /// Initializes a new instance of <see cref="Observers"/>.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to work with instances of <see cref="IObserver"/> types.</param>
        /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
        /// <param name="channel"><see cref="GrpcChannel"/> to use.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public Observers(IServiceProvider serviceProvider, IEventTypes eventTypes, IGrpcChannel channel, ITypes types)
        {
            _observers = types.All
                                .Where(_ => _.HasAttribute<ObserverAttribute>())
                                .Select(_ =>
                                {
                                    var observer = _.GetCustomAttribute<ObserverAttribute>()!;
                                    return new Observer(
                                        observer.ObserverId,
                                        observer.EventLogId,
                                        eventTypes,
                                        new ObserverInvoker(serviceProvider, eventTypes, _),
                                        channel);
                                });
        }

        /// <inheritdoc/>
        public void StartObserving()
        {
            foreach (var observer in _observers)
            {
                observer.StartObserving();
            }
        }
    }
}
