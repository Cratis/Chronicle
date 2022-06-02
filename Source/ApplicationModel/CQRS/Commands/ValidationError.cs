// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents the an failed validation rule.
/// </summary>
/// <param name="Message">Message of the error.</param>
/// <param name="MemberNames">Collection of member names that caused the failure.</param>
public record ValidationError(string Message, IEnumerable<string> MemberNames);
