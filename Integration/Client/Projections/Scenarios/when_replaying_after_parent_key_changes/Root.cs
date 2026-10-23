// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes;

public class Root
{
    public string RootId { get; set; } = string.Empty;
    public List<Child> Children { get; set; } = [];
}
