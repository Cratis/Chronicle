// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.ReadModels;

public class Child
{
    public ChildId ChildId { get; set; }
    public string Name { get; set; } = string.Empty;
}
