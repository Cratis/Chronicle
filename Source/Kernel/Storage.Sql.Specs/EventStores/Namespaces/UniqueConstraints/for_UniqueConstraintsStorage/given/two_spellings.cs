// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

/// <summary>
/// Two spellings of an owner that the default collation of SQL Server treats as one, each holding its own claim.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_spellings(SqlServerFixture fixture, string first, string second) : a_server_storage
{
    protected string _first = first;
    protected string _second = second;
    protected string _firstValue = new('a', 64);
    protected string _secondValue = new('b', 64);
    protected Exception? _firstError;
    protected Exception? _secondError;
    protected bool _firstValueRefusedForSecond;
    protected bool _secondValueRefusedForFirst;
    protected bool _secondOwnsItsValue;
    protected bool _firstValueAvailableAfterRemoval;
    protected bool _secondValueStillHeld;
    protected string[] _heldBySecond = [];
    protected long _rows;
    protected long _collapsedByCollation;

    protected async Task Prepare()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateStorage(connectionString, true, () => fixture.QueueDatabaseDrop(connectionString));
    }

    protected async Task SaveBoth()
    {
        _collapsedByCollation = await Scalar($"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END");
        _firstError = await Catch.Exception(() => _storage.Save(_first, _definition, 42UL, _firstValue));
        _secondError = await Catch.Exception(() => _storage.Save(_second, _definition, 43UL, _secondValue));
        _rows = await _context.Entries.CountAsync();
        (var firstAllowed, _) = await _storage.IsAllowed(_second, _definition, _firstValue);
        _firstValueRefusedForSecond = !firstAllowed;
        (var secondAllowed, _) = await _storage.IsAllowed(_first, _definition, _secondValue);
        _secondValueRefusedForFirst = !secondAllowed;
        (_secondOwnsItsValue, _) = await _storage.IsAllowed(_second, _definition, _secondValue);
        _heldBySecond = (await _storage.GetFor(_second, _definition)).Select(_ => _.Value).ToArray();
        await _storage.Remove(_first, _definition);
        (_firstValueAvailableAfterRemoval, _) = await _storage.IsAllowed(_second, _definition, _firstValue);
        (var held, _) = await _storage.IsAllowed(_first, _definition, _secondValue);
        _secondValueStillHeld = !held;
    }
}
