// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the response from starting a capture.
/// </summary>
[ProtoContract]
public class StartCaptureResponse
{
    /// <summary>
    /// Gets or sets the messages preventing the start - empty when the capture was started.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<CaptureValidationMessage> Messages { get; set; } = [];
}
