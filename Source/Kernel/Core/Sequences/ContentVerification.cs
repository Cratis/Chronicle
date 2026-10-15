// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of verifying stored event content.
/// </summary>
/// <param name="Result">The verification result.</param>
public record ContentVerification(ContentVerificationResult Result);
