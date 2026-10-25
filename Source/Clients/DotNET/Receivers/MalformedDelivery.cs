// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Indicates that the webhook body or its target is malformed.
/// </summary>
/// <param name="reason">A safe reason that contains no payload data.</param>
public sealed class MalformedDelivery(string reason) : Exception(reason);
