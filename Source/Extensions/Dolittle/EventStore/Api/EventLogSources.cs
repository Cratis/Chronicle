// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle.EventStore.Api
{
    [Route("/api/events/store/log/sources")]
    public class EventLogSources : Controller
    {
        [HttpGet]
        public Task<IEnumerable<string>> AllEventSources()
        {
            return Task.FromResult(Array.Empty<string>() as IEnumerable<string>);
        }
    }
}
