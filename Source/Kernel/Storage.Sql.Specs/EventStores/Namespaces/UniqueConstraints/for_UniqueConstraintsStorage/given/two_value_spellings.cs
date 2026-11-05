// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

/// <summary>
/// Two spellings of an owner that the default collation of SQL Server treats as one, each holding its own claim in a per-value table.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_value_spellings(SqlServerFixture fixture, string first, string second) : a_server_claims_storage(fixture)
{
    protected string _first = first;
    protected string _second = second;
    protected string _firstValue = new('a', 64);
    protected string _secondValue = new('b', 64);
    protected long _collapsedByCollation;
    protected Exception? _firstError;
    protected Exception? _secondError;
    protected long _rows;
    protected bool _otherSpellingMayTakeTheValue;
    protected string[] _heldByFirst = [];
    protected string[] _streamedBySecond = [];
    protected long _rowsAfterForeignRemoveValue;
    protected long _rowsAfterRemoveValue;
    protected bool _secondStillOwnsItsValue;
    protected long _rowsAfterRemove;
    protected bool _firstValueFree;

    protected async Task SaveAndRemove()
    {
        _collapsedByCollation = await Scalar($"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END");
        _firstError = await Catch.Exception(() => _storage.Save(_first, _perValue, 42UL, _firstValue));
        _secondError = await Catch.Exception(() => _storage.Save(_second, _perValue, 43UL, _secondValue));
        _rows = await Rows(_valuesTable);
        (_otherSpellingMayTakeTheValue, _) = await _storage.IsAllowed(_second, _perValue, _firstValue);
        _heldByFirst = (await _storage.GetFor(_first, _perValue)).Select(_ => _.Value).ToArray();
        var streamed = new List<string>();
        await foreach (var value in _storage.GetForStream(_second, _perValue))
        {
            streamed.Add(value.Value);
        }
        _streamedBySecond = [.. streamed];

        await _storage.RemoveValue(_second, _perValue, _firstValue);
        _rowsAfterForeignRemoveValue = await Rows(_valuesTable);
        await _storage.RemoveValue(_first, _perValue, _firstValue);
        _rowsAfterRemoveValue = await Rows(_valuesTable);
        (_secondStillOwnsItsValue, _) = await _storage.IsAllowed(_second, _perValue, _secondValue);

        await _storage.Save(_first, _perValue, 44UL, _firstValue);
        await _storage.Remove(_first, _perValue);
        _rowsAfterRemove = await Rows(_valuesTable);
        (_firstValueFree, _) = await _storage.IsAllowed(_second, _perValue, _firstValue);
    }
}
