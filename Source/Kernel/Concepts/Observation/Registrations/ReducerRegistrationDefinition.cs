// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Preserves the complete reducer definition, including read model and schema fingerprint.
/// </summary>
/// <param name="Definition">The reducer definition.</param>
public record ReducerRegistrationDefinition(ReducerDefinition Definition) : ObserverRegistrationDefinition;
