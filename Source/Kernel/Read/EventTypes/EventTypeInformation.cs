// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Read.EventTypes;

/// <summary>
/// Represents information about an event type.
/// </summary>
/// <param name="Identifier">The identifier of the event type.</param>
/// <param name="Name">Name of the event type.</param>
/// <param name="Generations">Number of generations.</param>
public record EventTypeInformation(string Identifier, string Name, uint Generations);
