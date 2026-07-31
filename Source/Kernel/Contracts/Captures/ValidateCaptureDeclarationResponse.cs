// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the response from validating a capture declaration.
/// </summary>
[ProtoContract]
public class ValidateCaptureDeclarationResponse
{
    /// <summary>
    /// Gets or sets the messages - empty when the declaration is valid.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<CaptureValidationMessage> Messages { get; set; } = [];
}
