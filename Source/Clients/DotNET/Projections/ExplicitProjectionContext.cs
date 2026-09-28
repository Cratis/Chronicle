// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents what an <see cref="IExplicitProjection"/> needs from its event store to build its definition.
/// </summary>
/// <param name="NamingPolicy">The <see cref="INamingPolicy"/> in use.</param>
/// <param name="EventTypes">The <see cref="IEventTypes"/> known to the event store.</param>
/// <param name="JsonSerializerOptions">The <see cref="JsonSerializerOptions"/> in use.</param>
/// <param name="EventStoreName">The name of the event store the projection belongs to, if known.</param>
internal record ExplicitProjectionContext(
    INamingPolicy NamingPolicy,
    IEventTypes EventTypes,
    JsonSerializerOptions JsonSerializerOptions,
    string? EventStoreName);
