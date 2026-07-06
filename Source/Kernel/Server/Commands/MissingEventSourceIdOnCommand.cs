// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Commands;

/// <summary>
/// The exception that is thrown when a command return value is recognized as an event but the originating
/// command has no resolvable <see cref="Concepts.Events.EventSourceId"/>.
/// </summary>
/// <param name="commandType">The originating command type.</param>
public sealed class MissingEventSourceIdOnCommand(Type commandType)
    : Exception($"Command type '{commandType.FullName}' returned an event but does not expose an EventSourceId property or a property marked with [Key].");
