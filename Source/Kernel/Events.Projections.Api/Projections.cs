// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
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
            var projections = new List<Projection>();
            var merged = _projections.Pipelines
                .SelectMany(pipeline =>
                    pipeline.State.CombineLatest(
                        pipeline.Positions,
                        pipeline.Jobs.Added,
                        pipeline.Jobs.Removed,
                        (state, positions, addedJob, removedJob) =>
                        {
                            var stateString = Enum.GetName(typeof(ProjectionState), state) ?? "[N/A]";
                            var positionsString = string.Join("-", positions.Values);

                            return addedJob.Status.Progress.CombineLatest(
                                addedJob.Status.Task,
                                (_, __) =>
                                {
                                    var jobInformation = string.Join("\n", pipeline.Jobs.Select(_ => $"{_.Status.Progress.Value} - {_.Status.Task.Value}"));
                                    return new Projection(pipeline.Projection.Identifier, pipeline.Projection.Name, stateString, jobInformation, positionsString);
                                });
                        })).Switch();
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

        [HttpPost("{projectionId}/rewind")]
        public void Rewind([FromRoute] Guid projectionId)
        {
            _projections.GetById(projectionId).Rewind();
        }

        [HttpGet("{projectionId}/collections")]
        public IEnumerable<ProjectionCollection> Collections([FromRoute] Guid projectionId)
        {
            return new ProjectionCollection[] {
                new("Something", 42)
            };
        }
    }
}
