// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Holds the HTTP delivery v1 media type.
/// </summary>
public static class DeliveryMediaType
{
    /// <summary>
    /// The versioned JSON media type.
    /// </summary>
    public const string Value = "application/vnd.cratis.chronicle.delivery+json; v=1";
}
