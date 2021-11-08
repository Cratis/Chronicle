// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Api
{
    [Route("/api/events/projections")]
    public class Projections : Controller
    {
        readonly ILogger<Projections> _logger;

        public Projections(ILogger<Projections> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public Task<IEnumerable<Projection>> GetAll()
        {
            _logger.LogInformation($"Get all projections");
            var projections = new Projection[] {
                new Projection("Something", 0)
            }.AsEnumerable();
            return Task.FromResult(projections);
        }
    }
}
