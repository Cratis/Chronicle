// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

[Route("specs/unrelated")]
public class UnrelatedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
