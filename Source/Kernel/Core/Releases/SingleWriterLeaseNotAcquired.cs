// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Indicates that startup could not acquire exclusive access to kernel storage.
/// </summary>
/// <param name="holder">The last known holder.</param>
public class SingleWriterLeaseNotAcquired(SingleWriterLeaseHolder? holder)
    : Exception($"Single-writer lease not acquired. Holder: {holder?.Owner.Value ?? "unknown"}, host: {holder?.HostName ?? "unknown"}, expires: {holder?.ExpiresAt:O}.");
