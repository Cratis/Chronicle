// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;

public class Hub
{
    [Index]
    public HubId HubId { get; set; } = HubId.NotSet;

    public string Name { get; set; } = string.Empty;
}
