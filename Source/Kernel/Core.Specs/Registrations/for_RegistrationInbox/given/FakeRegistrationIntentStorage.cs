// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox.given;

/// <summary>
/// Keeps intents in memory and counts the writes, standing in for the durable storage.
/// </summary>
public class FakeRegistrationIntentStorage : IRegistrationIntentStorage
{
    readonly Lock _lock = new();
    readonly Dictionary<(RegistrationKind, string), RegistrationIntent> _intents = [];
    int _saves;

    public int Saves => _saves;

    public IReadOnlyList<RegistrationIntent> Intents
    {
        get
        {
            lock (_lock)
            {
                return [.. _intents.Values];
            }
        }
    }

    public void Seed(RegistrationIntent intent) => _intents[(intent.Kind, intent.Key)] = intent;

    public Task<IReadOnlyList<RegistrationIntent>> GetAll() => Task.FromResult(Intents);

    public Task Save(RegistrationIntent intent)
    {
        lock (_lock)
        {
            _intents[(intent.Kind, intent.Key)] = intent;
        }

        Interlocked.Increment(ref _saves);
        return Task.CompletedTask;
    }
}
