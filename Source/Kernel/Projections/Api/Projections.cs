// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Aksio.Cratis.Applications.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Events.Projections.Api
{
    /// <summary>
    /// Represents the API for projections.
    /// </summary>
    [Route("/api/events/projections")]
    public class Projections : Controller
    {
        readonly IProjections _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// </summary>
        /// <param name="projections">Underlying <see cref="IProjections"/>.</param>
        public Projections(IProjections projections)
        {
            _projections = projections;
        }

        /// <summary>
        /// Gets all projections.
        /// </summary>
        /// <returns><see cref="ClientObservable{T}"/> containing all projections.</returns>
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
                                    return new Projection(
                                        pipeline.Projection.Identifier,
                                        pipeline.Projection.Name,
                                        pipeline.Projection.IsPassive,
                                        pipeline.Projection.IsRewindable,
                                        stateString,
                                        jobInformation,
                                        positionsString);
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

        /// <summary>
        /// Rewind a specific projection.
        /// </summary>
        /// <param name="projectionId">Id of projection to rewind.</param>
        [HttpPost("{projectionId}/rewind")]
        public void Rewind([FromRoute] Guid projectionId)
        {
            _projections.GetById(projectionId).Rewind();
        }

        /// <summary>
        /// Get all collections for projection.
        /// </summary>
        /// <param name="projectionId">Id of projection to get for.</param>
        /// <returns>Collection of all the projection collections.</returns>
        [HttpGet("{projectionId}/collections")]
        #pragma warning disable IDE0060
        public IEnumerable<ProjectionCollection> Collections([FromRoute] Guid projectionId)
        {
            return new ProjectionCollection[]
            {
                new("Something", 42)
            };
        }
    }
}
