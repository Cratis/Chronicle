// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The collaborators shared while building the children definitions of a model-bound projection.
/// </summary>
/// <param name="GetOrCreateEventType">Function to get or create a cached EventType instance.</param>
/// <param name="NamingPolicy">The naming policy for converting property names.</param>
/// <param name="ProcessMember">The action to process child members recursively.</param>
/// <param name="Definition">The root projection definition for processing member attributes.</param>
internal sealed record ChildrenProcessingContext(
    Func<Type, EventType> GetOrCreateEventType,
    INamingPolicy NamingPolicy,
    Action<MemberInfo, ProjectionDefinition, List<Attribute>, bool, Type?, ChildrenDefinition?> ProcessMember,
    ProjectionDefinition Definition);
