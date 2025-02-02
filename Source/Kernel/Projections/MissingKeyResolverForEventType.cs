// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when there is no <see cref="ValueProvider{Event}"/> providing the key value for a specific <see cref="EventType"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingKeyResolverForEventType"/>.
/// </remarks>
/// <param name="eventType">The <see cref="EventType"/>.</param>
public class MissingKeyResolverForEventType(EventType eventType) : Exception($"Missing key resolver for '{eventType}'")
{
}
