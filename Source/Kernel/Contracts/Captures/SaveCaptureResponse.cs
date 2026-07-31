// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the response from saving a capture.
/// </summary>
[ProtoContract]
public class SaveCaptureResponse
{
    /// <summary>
    /// Gets or sets the saved <see cref="Capture"/> - null when the save was rejected.
    /// </summary>
    [ProtoMember(1)]
    public Capture? Capture { get; set; }

    /// <summary>
    /// Gets or sets the messages explaining why the save was rejected - empty when the capture was saved.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<CaptureValidationMessage> Messages { get; set; } = [];
}
