// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an endpoint for the Cratis Kernel to probe for liveness.
/// </summary>
[Route("/.cratis/client")]
public class ClientCallback : Controller
{
    /// <summary>
    /// Ping action for Kernel to call on a regular basis to check for liveness.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [HttpGet("ping")]
    public Task Ping() => Task.CompletedTask;
}
