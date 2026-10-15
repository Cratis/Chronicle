// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_projecting_fluent_variants;

/// <summary>
/// Anchors the logical identity shared by <see cref="FluentBacklogItem"/> and <see cref="FluentPullRequestItem"/>.
/// </summary>
public class FluentWorkItem;

/// <summary>
/// The fluent variant an entity is in before a pull request exists for it.
/// </summary>
/// <param name="Id">The entity's identity, shared by every variant.</param>
/// <param name="Title">The entity's title.</param>
public record FluentBacklogItem(Guid Id, string Title);

/// <summary>
/// The fluent variant an entity enters once a pull request is created for it.
/// </summary>
/// <param name="Id">The entity's identity, shared by every variant.</param>
/// <param name="PullRequestUrl">The URL of the pull request that moved the entity into this variant.</param>
/// <param name="BuildStatus">The most recent build status reported for the pull request.</param>
public record FluentPullRequestItem(Guid Id, string PullRequestUrl, string BuildStatus);

/// <summary>
/// Projects <see cref="FluentBacklogItem"/>, entered on <see cref="IssueCreated"/>.
/// </summary>
public class FluentBacklogItemProjection : IProjectionFor<FluentBacklogItem>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentBacklogItem> builder) => builder
        .VariantOf<FluentWorkItem>(_ => _.Id)
        .EntersOn<IssueCreated>();
}

/// <summary>
/// Projects <see cref="FluentPullRequestItem"/>, entered on <see cref="PullRequestCreated"/>.
/// </summary>
public class FluentPullRequestItemProjection : IProjectionFor<FluentPullRequestItem>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentPullRequestItem> builder) => builder
        .VariantOf<FluentWorkItem>(_ => _.Id)
        .EntersOn<PullRequestCreated>()
        .From<BuildCompleted>();
}
