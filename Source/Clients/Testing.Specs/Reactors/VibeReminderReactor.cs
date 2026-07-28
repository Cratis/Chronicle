// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor that returns a <see cref="SendReminder"/> command as a side effect when a vibe is started —
/// the functional "return the command" style — used to verify ReactorScenario records produced commands.
/// </summary>
public class VibeReminderReactor : IReactor
{
    /// <summary>
    /// Reacts to a <see cref="VibeStarted"/> event by producing a <see cref="SendReminder"/> command.
    /// </summary>
    /// <param name="event">The triggering <see cref="VibeStarted"/> event.</param>
    /// <returns>A <see cref="SendReminder"/> command targeting the vibe's host.</returns>
    public Task<SendReminder> VibeStarted(VibeStarted @event) => Task.FromResult(new SendReminder(@event.Host));
}
