// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines why a webhook signature was not accepted.
/// </summary>
public enum WebhookSignatureFailure
{
    /// <summary>
    /// The signature was accepted.
    /// </summary>
    None = 0,

    /// <summary>
    /// A required header was missing or empty.
    /// </summary>
    MissingHeader = 1,

    /// <summary>
    /// The timestamp was not a canonical Unix seconds integer.
    /// </summary>
    InvalidTimestamp = 2,

    /// <summary>
    /// The timestamp was further from the current time than the tolerance allows.
    /// </summary>
    TimestampOutsideTolerance = 3,

    /// <summary>
    /// The message identifier was invalid, because it contains a <c language="text">.</c> or cannot be encoded as UTF-8.
    /// </summary>
    InvalidId = 4,

    /// <summary>
    /// The signature header held no signature of a supported version.
    /// </summary>
    NoSupportedSignature = 5,

    /// <summary>
    /// No supported signature was valid for any configured key, or the header exceeded the size limits.
    /// </summary>
    SignatureMismatch = 6
}
