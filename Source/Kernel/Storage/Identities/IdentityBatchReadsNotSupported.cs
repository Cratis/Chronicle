// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Identities;

/// <summary>
/// Thrown when a storage provider does not support uncached identity batch reads.
/// </summary>
public class IdentityBatchReadsNotSupported() : Exception("This storage provider does not support uncached identity batch reads.");
