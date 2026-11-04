// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset;

public class and_a_protected_value_cannot_be_located : given.a_future_changeset
{
    Exception? _exception;

    void Establish() => _changeset.Add(new PropertiesChanged<ExpandoObject>(_state,
        [new PropertyDifference("[children].email", null, Email, new ArrayIndexers([new ArrayIndexer("[children]", "id", Guid.NewGuid())]))]));

    async Task Because() => _exception = await Catch.Exception(async () => await _step.ProtectFutureChangeset(_projection, _context));

    [Fact] void should_fail_with_future_value_could_not_be_protected() => _exception.ShouldBeOfExactType<FutureValueCouldNotBeProtected>();
}
