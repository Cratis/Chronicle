// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/store/logs")]
    public class EventLogs : Controller
    {
        [HttpGet]
        public Task<IEnumerable<string>> AllEventLogs()
        {
            return Task.FromResult(Array.Empty<string>() as IEnumerable<string>);
        }
    }
}
