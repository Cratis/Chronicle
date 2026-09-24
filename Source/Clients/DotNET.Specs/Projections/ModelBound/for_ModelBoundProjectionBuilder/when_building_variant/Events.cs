// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

[EventType]
public record IssueCreated(string Title);

[EventType]
public record IssueStarted;

[EventType]
public record PullRequestCreated(string PullRequestUrl);

[EventType]
public record BuildCompleted(string BuildStatus);

[EventType]
public record TitleChanged(string Title);
