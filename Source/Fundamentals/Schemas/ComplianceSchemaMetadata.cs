// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Schemas
{
    /// <summary>
    /// Represents the compliance metadata stored in a schema.
    /// </summary>
    /// <param name="metadataType">Type of metadata.</param>
    /// <param name="details">Details.</param>
    public record ComplianceSchemaMetadata(Guid metadataType, string details);
}
