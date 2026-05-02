// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Compliance;

/// <summary>
/// Defines the gRPC service contract for compliance operations.
/// </summary>
[Service]
public interface ICompliance
{
    /// <summary>
    /// Release (decrypt) PII-annotated properties in a JSON payload using the given subject as the key identifier.
    /// </summary>
    /// <param name="request">The <see cref="ReleaseRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="ReleaseResponse"/> with the decrypted payload or an error.</returns>
    [Operation]
    Task<ReleaseResponse> Release(ReleaseRequest request, CallContext context = default);
}
