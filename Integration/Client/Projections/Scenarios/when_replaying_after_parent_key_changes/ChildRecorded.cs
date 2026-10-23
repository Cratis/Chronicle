// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes;

[EventType]
public record ChildRecorded(string ParentId, string ChildId, string Name);
