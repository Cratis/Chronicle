// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Exception that gets thrown when where is no <see cref="ComplianceMetadata"/> associated with a property.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NoComplianceMetadataForProperty"/> class.
/// </remarks>
/// <param name="type"><see cref="Type"/> that does not have compliance metadata.</param>
public class NoComplianceMetadataForType(Type type) : Exception($"Types '{type.FullName}'  does not have any compliance metadata.")
{
}
