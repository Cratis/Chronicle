// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the outcome of validating a capture declaration.
/// </summary>
/// <param name="Messages">What compiling and validating the declaration had to say.</param>
public record ValidateCaptureDeclarationResult(IEnumerable<Contracts.Captures.CaptureValidationMessage> Messages);
