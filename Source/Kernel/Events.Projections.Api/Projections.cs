// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
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
        public ClientObservable<IEnumerable<Projection>> AllProjections()
        {
            var observable = new ClientObservable<IEnumerable<Projection>>();
            var subscription = _projections.All.Subscribe(_ => observable.OnNext(Convert(_)));
            observable.ClientDisconnected = () => subscription.Dispose();
            return observable;
        }

        [HttpPost("rewind/{projectionId}")]
        public void Rewind([FromRoute] Guid projectionId)
        {
            _projections.GetById(projectionId).Rewind();
        }

        IEnumerable<Projection> Convert(IEnumerable<IProjectionPipeline> pipelines) =>
            pipelines.Select(_ =>
                new Projection(
                    _.Projection.Identifier,
                    _.Projection.Name,
                    Enum.GetName(typeof(ProjectionState), _.CurrentState) ?? "Unknown",
                    string.Join("-", _.CurrentPositions.Values)));
    }
}
