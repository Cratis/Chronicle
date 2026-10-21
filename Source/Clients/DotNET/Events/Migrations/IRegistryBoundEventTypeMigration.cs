// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Identifies the actual typed-base constructor path, independently of nomination or constructor DI parameters.
/// </summary>
internal interface IRegistryBoundEventTypeMigration
{
    /// <summary>
    /// Gets the explicitly supplied owning registry, or null for the unchanged declaration-only constructor path.
    /// </summary>
    IEventTypes? Registry { get; }
}
