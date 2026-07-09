// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event starting an engagement for a company identified by its string organization number.
/// </summary>
/// <param name="CustomerOrgNumber">The company the engagement is for (the string join key).</param>
[EventType]
public record EngagementStarted(OrgNumber CustomerOrgNumber);
