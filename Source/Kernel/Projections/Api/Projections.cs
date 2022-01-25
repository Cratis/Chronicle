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

            var statusSubscriptions = new List<IDisposable>();
            var subscription = _projections.Pipelines.Subscribe(pipelines =>
            {
                statusSubscriptions.ForEach(_ => _.Dispose());

                var projections = pipelines.Select(p => new Projection(
                        p.Projection.Identifier,
                        p.Projection.Name,
                        p.Projection.IsPassive,
                        p.Projection.IsRewindable,
                        string.Empty,
                        string.Empty,
                        string.Empty)).ToList();

                observable.OnNext(projections);

                foreach (var statusObservable in pipelines.Select(_ => _.Status))
                {
                    statusObservable.Subscribe(status =>
                    {
                        var stateString = Enum.GetName(typeof(ProjectionState), status.State) ?? "[N/A]";
                        var positionsString = string.Join("-", status.Positions.Values);
                        var jobInformation = string.Join("\n", status.Jobs.Select(_ => $"{_.Status.Progress.Value} - {_.Status.Task.Value}"));

                        var newItem = new Projection(
                            status.Projection!.Identifier,
                            status.Projection!.Name,
                            status.Projection!.IsPassive,
                            status.Projection!.IsRewindable,
                            stateString,
                            jobInformation,
                            positionsString);

                        var existing = projections.Find(p => p.Id == status.Projection?.Identifier.Value);
                        if (existing != default)
                        {
                            var index = projections.IndexOf(existing);
                            projections.Remove(existing);
                            projections.Insert(index, newItem);
                        }
                        else
                        {
                            projections.Add(newItem);
                        }

                        observable.OnNext(projections);
                    });
                }
            });

            observable.ClientDisconnected = () =>
            {
                subscription.Dispose();
                statusSubscriptions.ForEach(_ => _.Dispose());
            };
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
