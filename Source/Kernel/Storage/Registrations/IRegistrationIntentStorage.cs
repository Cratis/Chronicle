// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Registrations;

/// <summary>
/// Defines the storage of the registrations the kernel has accepted and applies in the background.
/// </summary>
public interface IRegistrationIntentStorage
{
    /// <summary>
    /// Gets every intent of the event store.
    /// </summary>
    /// <returns>The <see cref="RegistrationIntent"/> records.</returns>
    Task<IReadOnlyList<RegistrationIntent>> GetAll();

    /// <summary>
    /// Saves an intent in a single write, replacing the intent that has the same kind and key.
    /// </summary>
    /// <param name="intent">The <see cref="RegistrationIntent"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(RegistrationIntent intent);
}
