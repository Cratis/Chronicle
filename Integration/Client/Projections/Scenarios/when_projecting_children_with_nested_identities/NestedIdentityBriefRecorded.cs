// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

/// <summary>
/// A brief was recorded for a child of the event source.
/// </summary>
/// <param name="Brief">The recorded brief.</param>
[EventType]
public record NestedIdentityBriefRecorded(NestedIdentityBrief Brief);
