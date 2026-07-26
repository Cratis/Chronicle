// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A service the <see cref="VibeCancellationReactor"/> takes as a constructor dependency, used to verify that
/// constructor dependencies registered in <see cref="ReactorScenario{TReactor}.Services"/> are resolved.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notifies the given host.
    /// </summary>
    /// <param name="host">The host to notify.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Notify(string host);
}
