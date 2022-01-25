// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents a <see cref="IHostedService"/> for working with projections.
    /// </summary>
    public class ProjectionsService : IHostedService
    {
        readonly IProjectionsRegistrar _projectionsRegistrar;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionsService"/> class.
        /// </summary>
        /// <param name="projectionsRegistrar">The <see cref="IProjectionsRegistrar"/> system.</param>
        public ProjectionsService(IProjectionsRegistrar projectionsRegistrar)
        {
            _projectionsRegistrar = projectionsRegistrar;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _projectionsRegistrar.StartAll();
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
