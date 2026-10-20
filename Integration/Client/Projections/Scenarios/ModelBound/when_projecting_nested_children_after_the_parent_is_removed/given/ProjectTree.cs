// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.given;

[EventType]
public record TreeProjectRegistered(string Name);

[EventType]
public record TreeProjectRemoved;

[EventType]
public record TreeApplicationCreatedForProject(Guid ProjectId, string Name);

[EventType]
public record TreeApplicationRemovedFromProject(Guid ProjectId);

[EventType]
public record TreeDomainCreatedForApplication(Guid ApplicationId, string Name);

[EventType]
public record TreeDomainRenamed(Guid DomainId, Guid ApplicationId, string NewName);

[EventType]
public record TreeDomainRemoved(Guid DomainId, Guid ApplicationId);

[EventType]
public record TreeEventModelCreatedForDomain(Guid DomainId, string Name);

[FromEvent<TreeEventModelCreatedForDomain>]
public record TreeEventModel(Guid Id, string Name);

[FromEvent<TreeDomainCreatedForApplication>]
[FromEvent<TreeDomainRenamed>(key: nameof(TreeDomainRenamed.DomainId), parentKey: nameof(TreeDomainRenamed.ApplicationId))]
[RemovedWith<TreeDomainRemoved>(key: nameof(TreeDomainRemoved.DomainId), parentKey: nameof(TreeDomainRemoved.ApplicationId))]
public record TreeDomain(
    Guid DomainId,
    [NoAutoMap]
    [SetFrom<TreeDomainCreatedForApplication>(nameof(TreeDomainCreatedForApplication.Name))]
    [SetFrom<TreeDomainRenamed>(nameof(TreeDomainRenamed.NewName))]
    string Name,
    [ChildrenFrom<TreeEventModelCreatedForDomain>(parentKey: nameof(TreeEventModelCreatedForDomain.DomainId), identifiedBy: nameof(TreeEventModel.Id))]
    IEnumerable<TreeEventModel> EventModels);

[FromEvent<TreeApplicationCreatedForProject>]
[RemovedWith<TreeApplicationRemovedFromProject>(parentKey: nameof(TreeApplicationRemovedFromProject.ProjectId))]
public record TreeApplication(
    Guid Id,
    string Name,
    [ChildrenFrom<TreeDomainCreatedForApplication>(parentKey: nameof(TreeDomainCreatedForApplication.ApplicationId), identifiedBy: nameof(TreeDomain.DomainId))]
    IEnumerable<TreeDomain> Domains);

[FromEvent<TreeProjectRegistered>]
[RemovedWith<TreeProjectRemoved>]
public record TreeProject(
    Guid Id,
    string Name,
    [ChildrenFrom<TreeApplicationCreatedForProject>(parentKey: nameof(TreeApplicationCreatedForProject.ProjectId), identifiedBy: nameof(TreeApplication.Id))]
    IEnumerable<TreeApplication> Applications);

#pragma warning restore SA1402, SA1649
