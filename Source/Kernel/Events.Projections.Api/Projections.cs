// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Aksio.Queries;
using Cratis.Events.Projections.Pipelines;
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
            var projections = new List<Projection>();
            var merged = _projections.Pipelines
                .Select(pipeline =>
                    pipeline.Positions
                        .Select(positions => string.Join("-", positions.Values))
                        .Select(_ => new Projection(pipeline.Projection.Identifier, pipeline.Projection.Name, "", _))
                ).Merge();
            var subscription = merged.Subscribe(projection =>
            {
                var existing = projections.Find(_ => _.Id == projection.Id);
                if (existing != default)
                {
                    var index = projections.IndexOf(existing);
                    projections.Remove(existing);
                    projections.Insert(index, projection);
                }
                else
                {
                    projections.Add(projection);
                }

                observable.OnNext(projections);
            });

            observable.ClientDisconnected = () => subscription.Dispose();
            return observable;
        }

        [HttpPost("rewind/{projectionId}")]
        public void Rewind([FromRoute] Guid projectionId)
        {
            _projections.GetById(projectionId).Rewind();
        }
    }
}
