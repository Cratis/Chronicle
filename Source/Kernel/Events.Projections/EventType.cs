// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the unique identifier of the type of an event.
    /// </summary>
    /// <param name="Value">The value.</param>
    public record EventType(string Value) : ConceptAs<string>(Value);
}
