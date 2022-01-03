// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Workbench.Compliance.Events.Microservices;

namespace Cratis.Workbench.Compliance.Read.Microservices
{
    [Observer("568786bb-cd1d-4cf0-b046-c7f42646e9f1")]
    public class ObserverTest
    {
        public Task MicroserviceAdded(MicroserviceAdded @event)
        {
            Console.WriteLine($"IT IS ADDED - {@event.Name}");
            return Task.CompletedTask;
        }
    }
}
