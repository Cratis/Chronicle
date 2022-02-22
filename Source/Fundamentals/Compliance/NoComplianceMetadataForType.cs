// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance;

/// <summary>
/// Exception that gets thrown when where is no <see cref="ComplianceMetadata"/> associated with a property.
/// </summary>
public class NoComplianceMetadataForType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoComplianceMetadataForProperty"/> class.
    /// </summary>
    /// <param name="type"><see cref="Type"/> that does not have compliance metadata.</param>
    public NoComplianceMetadataForType(Type type)
        : base($"Types '{type.FullName}'  does not have any compliance metadata.")
    {
    }
}
