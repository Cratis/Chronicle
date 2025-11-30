// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a migrator for a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type to migrate.</typeparam>
public interface IEventTypeMigrationFor<TEvent> : IEventTypeMigration
{

}
