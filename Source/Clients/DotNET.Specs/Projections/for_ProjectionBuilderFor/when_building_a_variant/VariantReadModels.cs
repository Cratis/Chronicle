// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

/// <summary>
/// Anchors the logical identity the variants share. Deliberately not a read model itself.
/// </summary>
public class WorkItem;

public record BacklogItem(Guid Id, string Title);

public record PullRequestItem(Guid Id, string PullRequestUrl, string BuildStatus);

[EventType]
public record IssueCreated(string Title);

[EventType]
public record PullRequestCreated(string PullRequestUrl);

[EventType]
public record BuildCompleted(string BuildStatus);
