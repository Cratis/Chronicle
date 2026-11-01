// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents the URL of an HTTP observer target.
/// </summary>
/// <param name="Value">The target URL.</param>
public record HttpTargetUrl(string Value) : ConceptAs<string>(Value);
