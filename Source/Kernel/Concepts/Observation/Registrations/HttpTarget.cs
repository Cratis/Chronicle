// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Describes a registered HTTP delivery endpoint without imposing delivery behavior.
/// </summary>
/// <param name="Url">The endpoint URL.</param>
/// <param name="Headers">The headers included with a delivery.</param>
public record HttpTarget(HttpTargetUrl Url, IDictionary<string, string> Headers);
