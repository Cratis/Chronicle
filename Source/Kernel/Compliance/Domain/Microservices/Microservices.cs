// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Events.Microservices;
using Aksio.Cratis.Events;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Compliance.Domain.Microservices;

/// <summary>
/// Represents the domain API for microservices.
/// </summary>
[Route("/api/compliance/microservices")]
public class Microservices : Controller
{
    readonly IEventLog _eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microservices"/> class.
    /// </summary>
    /// <param name="eventLog"><see cref="IEventLog"/> to work with.</param>
    public Microservices(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    /// <summary>
    /// Add a microservice.
    /// </summary>
    /// <param name="command"><see cref="AddMicroservice"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task AddMicroservice([FromBody] AddMicroservice command) => _eventLog.Append(command.MicroserviceId.Value, new MicroserviceAdded(command.Name));
}
