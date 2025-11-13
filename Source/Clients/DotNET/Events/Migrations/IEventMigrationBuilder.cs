// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Defines a builder for event migrations.
/// </summary>
public interface IEventMigrationBuilder
{
    /// <summary>
    /// Define property migrations.
    /// </summary>
    /// <param name="properties">Action to configure properties.</param>
    void Properties(Action<IEventMigrationPropertyBuilder> properties);
}
