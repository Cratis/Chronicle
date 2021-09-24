// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Schema;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/types")]
    public class EventTypes : Controller
    {
        [HttpGet]
        public Task<JSchema> AllEventTypes()
        {
            return Task.FromResult(new JSchema());
        }
    }
}
