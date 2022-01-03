// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Workbench.Compliance.Events.Microservices;

namespace Cratis.Workbench.Domain.Microservices
{
    [Route("/api/compliance/microservices")]
    public class Microservices : Controller
    {
        readonly IEventLog _eventLog;

        public Microservices(IEventLog eventLog)
        {
            _eventLog = eventLog;
        }

        [HttpPost]
        public Task AddMicroservice([FromBody] AddMicroservice command) => _eventLog.Append(command.MicroserviceId.Value, new MicroserviceAdded(command.Name));
    }
}
