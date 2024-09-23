// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore;

[Route("/api/something")]
public class MyController(IEventLog eventLog) : ControllerBase
{
    [HttpPost]
    public async Task Post()
    {
        await eventLog.Append(Guid.NewGuid().ToString(), new MyFirstEvent());
    }
}
