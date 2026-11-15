// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.given;

[EventType]
public record PersonRegistered(Guid PersonId, string Name);

[EventType]
public record PersonRemoved(Guid PersonId);

[EventType]
public record GroupCreated(Guid GroupId, string Name);

[EventType]
public record MemberAdded(Guid PersonId, Guid GroupId);

[EventType]
public record MemberRemoved(Guid PersonId, Guid GroupId);

[EventType]
public record PlaceVisited(Guid PersonId, Guid PlaceId, Guid VisitId);
