// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A string-backed concept value used by the EventScenario constraint specs.
/// </summary>
/// <param name="Value">The email address.</param>
public record SubscriberEmail(string Value) : ConceptAs<string>(Value);
