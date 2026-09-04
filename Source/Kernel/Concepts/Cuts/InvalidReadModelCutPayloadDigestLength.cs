// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Cuts;

/// <summary>
/// The exception that is thrown when a <see cref="ReadModelCutPayloadDigest"/> is constructed from a byte span that is not exactly 32 bytes long.
/// </summary>
/// <param name="actualLength">The actual number of bytes supplied.</param>
public class InvalidReadModelCutPayloadDigestLength(int actualLength)
    : Exception($"A read model cut payload digest must be exactly 32 bytes, but {actualLength} byte(s) were supplied");
