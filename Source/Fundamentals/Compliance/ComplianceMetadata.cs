// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Represents the metadata related to compliance.
    /// </summary>
    /// <param name="MetadataType">The <see cref="ComplianceMetadataType"/>.</param>
    /// <param name="Details">Any additional details - can be empty.</param>
    public record ComplianceMetadata(ComplianceMetadataType MetadataType, string Details);
}
