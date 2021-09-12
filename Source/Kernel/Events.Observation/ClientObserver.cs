// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;
using Orleans;

namespace Cratis.Events.Observation
{
    public class ClientObserver : Grain, IObserver
    {
        public Task Next(CommittedEvent @event) => throw new NotImplementedException();
    }
}
