// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

/// <summary>
/// Anchors the logical identity shared by the <see cref="BacklogItem"/>, <see cref="DevelopmentItem"/> and
/// <see cref="PullRequestItem"/> variants. Deliberately not a read model itself.
/// </summary>
public class WorkItem;

[VariantOf<WorkItem>]
[EntersOn<IssueCreated>]
public record BacklogItem([property: Key] Guid Id, string Title);

[VariantOf<WorkItem>]
[EntersOn<IssueStarted>]
public record DevelopmentItem([property: Key] Guid Id);

[VariantOf<WorkItem>]
[EntersOn<PullRequestCreated>]
public record PullRequestItem([property: Key] Guid Id, [property: SetFrom<PullRequestCreated>] string PullRequestUrl, [property: SetFrom<BuildCompleted>] string BuildStatus);

[VariantOf<WorkItem>]
public record MissingEntersOnVariant([property: Key] Guid Id);

[VariantOf<WorkItem>]
[EntersOn<IssueCreated>]
public record MissingKeyVariant(Guid Id);
