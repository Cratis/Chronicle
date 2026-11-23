// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetStoredProjectionDefinitions() => storage.GetEventStore(_eventStoreName).Projections.GetAll();
}
