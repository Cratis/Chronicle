// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Preserves the complete reactor definition, including generation delivery and start behavior.
/// </summary>
/// <param name="Definition">The reactor definition.</param>
public record ReactorRegistrationDefinition(ReactorDefinition Definition) : ObserverRegistrationDefinition;
