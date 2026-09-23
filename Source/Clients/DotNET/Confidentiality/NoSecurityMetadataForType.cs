// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Exception that gets thrown when where is no <see cref="SecurityMetadata"/> associated with a type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NoSecurityMetadataForType"/> class.
/// </remarks>
/// <param name="type"><see cref="Type"/> that does not have security metadata.</param>
public class NoSecurityMetadataForType(Type type) : Exception($"Type '{type.FullName}' does not have any security metadata.");
