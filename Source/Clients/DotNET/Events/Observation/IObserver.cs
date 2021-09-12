// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Defines the system that handles observations for a specific observer.
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// Start observing.
        /// </summary>
        void StartObserving();
    }
}
