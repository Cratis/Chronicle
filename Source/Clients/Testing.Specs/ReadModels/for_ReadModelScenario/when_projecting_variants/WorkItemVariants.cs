// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants;

/// <summary>
/// Anchors the logical identity shared by <see cref="BacklogItem"/> and <see cref="PullRequestItem"/>.
/// Deliberately not a read model itself.
/// </summary>
public class WorkItem;

[EventType]
public record IssueCreated(string Title);

[EventType]
public record PullRequestCreated(string PullRequestUrl);

[EventType]
public record BuildCompleted(string BuildStatus);

[EventType]
public record TitleChanged(string Title);

/// <summary>
/// The variant an entity is in before a pull request exists for it.
/// </summary>
/// <param name="Id">The entity's identity, shared by every variant.</param>
/// <param name="Title">The entity's title.</param>
[VariantOf<WorkItem>]
[EntersOn<IssueCreated>]
public record BacklogItem([property: Key] Guid Id, string Title);

/// <summary>
/// The variant an entity enters once a pull request is created for it. <see cref="BuildStatus"/> is
/// declared as a handler for <see cref="BuildCompleted"/> — an event that is NOT this variant's entering
/// event, so it must be reclassified into an update-only join and must never create the row on its own.
/// </summary>
/// <param name="Id">The entity's identity, shared by every variant.</param>
/// <param name="Title">The entity's title.</param>
/// <param name="PullRequestUrl">The URL of the pull request that moved the entity into this variant.</param>
/// <param name="BuildStatus">The most recent build status reported for the pull request.</param>
[VariantOf<WorkItem>]
[EntersOn<PullRequestCreated>]
public record PullRequestItem(
    [property: Key] Guid Id,
    string Title,
    [property: SetFrom<PullRequestCreated>] string PullRequestUrl,
    [property: SetFrom<BuildCompleted>] string BuildStatus);

/// <summary>
/// Declares a mapping every variant of <see cref="WorkItem"/> shares.
/// </summary>
/// <param name="Title">The title every variant carrying one keeps up to date.</param>
[GlobalFor<WorkItem>]
public record WorkItemSharedHandlers([property: SetFrom<TitleChanged>] string Title);
