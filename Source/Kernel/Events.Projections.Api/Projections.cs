// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Api
{
    [Route("/api/events/projections")]
    public class Projections : Controller
    {
        readonly IProjections _projections;
        readonly ILogger<Projections> _logger;

        public Projections(IProjections projections, ILogger<Projections> logger)
        {
            _projections = projections;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Projection> GetAll()
        {
            return _projections.GetAll().Select(_ => new Projection(_.Projection.Name, 0));
        }
    }
}
