// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

/// <summary>
/// Two spellings of a subject that the default collation of SQL Server treats as one, each holding claims in both table shapes.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_subject_spellings(SqlServerFixture fixture, string first, string second) : a_server_claims_storage(fixture)
{
    protected string _first = first;
    protected string _second = second;
    protected long _collapsedByCollation;
    protected long _sourceRowsBefore;
    protected long _valueRowsBefore;
    protected long _sourceRowsAfter;
    protected long _valueRowsAfter;
    protected long _sourceRowsOfFirst;
    protected long _valueRowsOfFirst;
    protected bool _secondSourceValueStillHeld;
    protected bool _secondValueStillHeld;
    protected bool _firstSourceValueFree;
    protected bool _firstValueFree;

    protected async Task SaveAndRemoveFirst()
    {
        _collapsedByCollation = await Scalar($"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END");
        await _storage.Save("owner-one", _perSource, 1UL, "source-one", subject: _first);
        await _storage.Save("owner-two", _perSource, 2UL, "source-two", subject: _second);
        await _storage.Save("owner-one", _perValue, 3UL, "value-one", subject: _first);
        await _storage.Save("owner-two", _perValue, 4UL, "value-two", subject: _second);
        _sourceRowsBefore = await Rows(_sourceTable);
        _valueRowsBefore = await Rows(_valuesTable);

        await _storage.RemoveForSubject(_first);

        _sourceRowsAfter = await Rows(_sourceTable);
        _valueRowsAfter = await Rows(_valuesTable);
        (_secondSourceValueStillHeld, _) = await _storage.IsAllowed("claimant", _perSource, "source-two");
        (_secondValueStillHeld, _) = await _storage.IsAllowed("claimant", _perValue, "value-two");
        (_firstSourceValueFree, _) = await _storage.IsAllowed("claimant", _perSource, "source-one");
        (_firstValueFree, _) = await _storage.IsAllowed("claimant", _perValue, "value-one");
    }
}
