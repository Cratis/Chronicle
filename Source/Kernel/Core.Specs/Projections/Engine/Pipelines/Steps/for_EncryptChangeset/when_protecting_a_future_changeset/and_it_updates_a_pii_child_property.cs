// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset;

public class and_it_updates_a_pii_child_property : given.a_future_changeset
{
    void Establish() => UpdateChild();

    async Task Because() => await _step.ProtectFutureChangeset(_projection, _context);

    [Fact] void should_keep_the_typed_child_identifier() => _indexers.All.Single().Identifier.ShouldBeOfExactType<Guid>();
    [Fact] void should_store_the_email_as_ciphertext() => Differences.Single(_ => _.PropertyPath == "[children].email").Changed.ShouldNotEqual(Email);
    [Fact] void should_keep_the_name_difference_unchanged() => Differences.Single(_ => _.PropertyPath == "[children].name").Changed.ShouldEqual("after");
    [Fact] void should_not_add_a_whole_collection_difference() => Differences.Any(_ => _.PropertyPath == "children" && _.ArrayIndexers.IsEmpty).ShouldBeFalse();
    [Fact] void should_record_the_subject_of_the_children() => Differences.Single(_ => _.PropertyPath == new PropertyPath("__subjects.children")).Changed.ShouldEqual("future-owner");
}
