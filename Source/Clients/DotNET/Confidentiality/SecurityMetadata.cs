// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Represents the metadata related to security.
/// </summary>
/// <param name="MetadataType">The <see cref="SecurityMetadataType"/>.</param>
/// <param name="Details">Any additional details - can be empty.</param>
/// <remarks>
/// This is the security counterpart to <see cref="Compliance.ComplianceMetadata"/> - a deliberately separate type
/// rather than a shared one. See <see cref="Schemas.SchemaMetadataCategory"/> for why.
/// </remarks>
public record SecurityMetadata(SecurityMetadataType MetadataType, SecurityMetadataDetails Details);
