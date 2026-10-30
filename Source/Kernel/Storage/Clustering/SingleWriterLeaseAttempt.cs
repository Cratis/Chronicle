// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SingleWriter;

/// <summary>
/// The outcome of an atomic acquisition attempt.
/// </summary>
/// <param name="Acquired">The Acquired.</param>
/// <param name="Holder">The Holder.</param>
public record SingleWriterLeaseAttempt(bool Acquired, SingleWriterLeaseHolder? Holder);
