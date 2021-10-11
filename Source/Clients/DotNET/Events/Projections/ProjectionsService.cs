// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a <see cref="IHostedService"/> for working with projections.
    /// </summary>
    public class ProjectionsService : IHostedService
    {
        readonly IProjections _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionsService"/> class.
        /// </summary>
        /// <param name="projections">The <see cref="IProjections"/> system.</param>
        public ProjectionsService(IProjections projections)
        {
            _projections = projections;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _projections.StartAll();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
