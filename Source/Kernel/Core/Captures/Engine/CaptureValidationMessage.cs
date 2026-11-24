// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents a message from validating a capture declaration beyond what the compiler can verify -
/// referenced external services, event types, scheduling and engine capabilities.
/// </summary>
/// <param name="Message">The message describing the problem.</param>
/// <param name="Line">The one-based line the problem relates to - 1 when the problem has no specific location.</param>
/// <param name="Column">The one-based column the problem relates to - 1 when the problem has no specific location.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Captures.Engine.CaptureValidationMessage")]
public record CaptureValidationMessage([Id(0)] string Message, [Id(1)] int Line = 1, [Id(2)] int Column = 1);
