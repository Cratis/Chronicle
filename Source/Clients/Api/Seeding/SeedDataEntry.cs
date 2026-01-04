// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a seeded event entry for API usage.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
/// <param name="IsGlobal">Whether this seed data is global (applies to all namespaces).</param>
/// <param name="TargetNamespace">The specific namespace this seed data applies to, if not global.</param>
public record SeedDataEntry(string EventSourceId, string EventTypeId, string Content, bool IsGlobal, string TargetNamespace);
