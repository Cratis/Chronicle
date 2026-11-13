// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Describes which side of a migration a validation runs for, and the generation it is validated against.
/// </summary>
/// <param name="EventTypeId">The event type the migration belongs to.</param>
/// <param name="Generation">The generation whose schema the migration side is validated against.</param>
/// <param name="Name">Either upcast or downcast.</param>
readonly record struct MigrationDirection(string EventTypeId, uint Generation, string Name);
