// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the definition of a configured external service.
/// </summary>
/// <param name="Id">The <see cref="ExternalServiceId"/>.</param>
/// <param name="Name">The human-readable <see cref="ExternalServiceName"/>.</param>
/// <param name="Endpoint">The <see cref="ExternalServiceEndpoint"/> describing how to connect.</param>
public record ExternalServiceDefinition(
    ExternalServiceId Id,
    ExternalServiceName Name,
    ExternalServiceEndpoint Endpoint);
