// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset;

public class and_it_adds_a_child_with_pii : given.a_future_changeset
{
    void Establish() => AddChild();

    async Task Because() => await _step.ProtectFutureChangeset(_projection, _context);

    [Fact] void should_encrypt_the_added_child_email_in_place() => ((IDictionary<string, object?>)_child)["email"].ShouldNotEqual(Email);
    [Fact] void should_keep_the_child_identifier() => ((IDictionary<string, object?>)_child)["id"].ShouldEqual(_childId);
}
