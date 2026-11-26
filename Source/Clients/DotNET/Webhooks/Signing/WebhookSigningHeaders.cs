// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// The names of the HTTP headers Chronicle sets on a signed webhook delivery.
/// </summary>
public static class WebhookSigningHeaders
{
    /// <summary>
    /// The header carrying the message identifier. It is part of the signed message.
    /// </summary>
    public const string Id = "webhook-id";

    /// <summary>
    /// The header carrying the attempt time in Unix seconds. It is part of the signed message.
    /// </summary>
    public const string Timestamp = "webhook-timestamp";

    /// <summary>
    /// The header carrying one or more space separated <c language="text">version,base64</c> signatures.
    /// </summary>
    public const string Signature = "webhook-signature";

    /// <summary>
    /// The header naming the key that produced the first signature. It is advisory only and never selects trust.
    /// </summary>
    public const string KeyId = "Cratis-Key-Id";
}
