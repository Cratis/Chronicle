// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Events.Projections.Api
{
    [Route("/api/events/projections")]
    public class Projections : Controller
    {
        readonly IProjections _projections;

        public Projections(IProjections projections)
        {
            _projections = projections;
        }

        [HttpGet]
        public IEnumerable<Projection> GetAll()
        {
            return _projections.GetAll().Select(_ =>
                new Projection(
                    _.Projection.Identifier,
                    _.Projection.Name,
                    Enum.GetName(typeof(ProjectionState), _.State) ?? "Unknown",
                    string.Join("-", _.Positions.Values)));
        }

        [HttpPost("rewind/{projectionId}")]
        public void Rewind([FromRoute] Guid projectionId)
        {
            _projections.GetById(projectionId).Rewind();
        }
    }
}
