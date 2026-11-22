// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Storage.InMemory.Registrations;

/// <summary>
/// Keeps registration intents in memory.
/// </summary>
public class RegistrationIntentStorage : IRegistrationIntentStorage
{
    readonly Lock _lock = new();
    readonly Dictionary<(RegistrationKind Kind, string Key), RegistrationIntent> _intents = [];

    /// <inheritdoc/>
    public Task<IReadOnlyList<RegistrationIntent>> GetAll()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<RegistrationIntent>>([.. _intents.Values]);
        }
    }

    /// <inheritdoc/>
    public Task Save(RegistrationIntent intent)
    {
        lock (_lock)
        {
            _intents[(intent.Kind, intent.Key)] = intent;
        }

        return Task.CompletedTask;
    }
}
