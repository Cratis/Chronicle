// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes;

public class Child
{
    public string ChildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
