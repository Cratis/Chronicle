// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.ReadModels;

public class Root
{
    public string Name { get; set; } = string.Empty;
    public List<Child> Children { get; set; } = [];
    public EventSequenceNumber __lastHandledEventSequenceNumber { get; set; }
}
