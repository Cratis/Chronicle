// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// A terminal business refusal, retained across attempts.
/// </summary>
/// <param name="Reasons">The reasons for refusal.</param>
public record Refused(IEnumerable<string> Reasons) : EffectOutcome;
