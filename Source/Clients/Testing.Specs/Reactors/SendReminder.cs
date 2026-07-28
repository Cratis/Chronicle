// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A stand-in command that <see cref="VibeReminderReactor"/> returns as a side effect, used to verify that
/// ReactorScenario records produced commands for assertion.
/// </summary>
/// <param name="Host">The host to remind.</param>
public record SendReminder(string Host);
