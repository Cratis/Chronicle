// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.GraphProjections.MultipleGraphs;

/// <summary>
/// The names of the people, owned by one graph projection.
/// </summary>
/// <param name="PersonId">The identifier of the person.</param>
/// <param name="Name">The name.</param>
[Graph("OrgNames")]
[Node]
[FromEvent<OrgPersonRegistered>]
[FromEvent<OrgPersonRenamed>]
public record Person([Key] Guid PersonId, string Name);
