// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Indicates that a delivery exceeds the local request body limit.
/// </summary>
/// <param name="maximumBodySize">The maximum accepted size in bytes.</param>
public sealed class DeliveryTooLarge(long maximumBodySize) : Exception($"Delivery body exceeds the local limit of {maximumBodySize} bytes.");
