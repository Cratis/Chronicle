// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public abstract class a_claim_lifecycle : a_server_storage
{
    protected Exception? _claimError;
    protected Exception? _moveError;
    protected bool _holderAllowed;
    protected bool _claimantAllowed;
    protected bool _oldValueAvailable;
    protected bool _releasedValueAvailable;
    protected EventSequenceNumber _oldSequenceNumber;
    protected int _claimCount;

    protected async Task SettleClaims()
    {
        var shared = new string('a', 64);
        var old = new string('b', 64);
        await _storage.Save("holder", _definition, 42UL, shared);
        _claimError = await Catch.Exception(() => _storage.Save("claimant", _definition, 43UL, shared));
        (_holderAllowed, _) = await _storage.IsAllowed("holder", _definition, shared);
        (_claimantAllowed, _) = await _storage.IsAllowed("claimant", _definition, shared);
        _claimCount = await _context.Entries.CountAsync();
        await _storage.Save("claimant", _definition, 44UL, old);
        _moveError = await Catch.Exception(() => _storage.Save("claimant", _definition, 45UL, shared));
        (_oldValueAvailable, _oldSequenceNumber) = await _storage.IsAllowed("third", _definition, old);
        await _storage.Remove("holder", _definition);
        (_releasedValueAvailable, _) = await _storage.IsAllowed("third", _definition, shared);
    }
}
