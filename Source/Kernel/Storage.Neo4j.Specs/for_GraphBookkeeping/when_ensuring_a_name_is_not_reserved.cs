// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphBookkeeping;

public class when_ensuring_a_name_is_not_reserved : Specification
{
    Exception _reserved;
    Exception _bookkeeping;
    Exception _allowed;

    void Because()
    {
        _reserved = Catch.Exception(() => GraphBookkeeping.EnsureNotReserved("_mine"));
        _bookkeeping = Catch.Exception(() => GraphBookkeeping.EnsureNotReserved(GraphBookkeeping.Watermark));
        _allowed = Catch.Exception(() => GraphBookkeeping.EnsureNotReserved("name"));
    }

    [Fact] void should_refuse_a_name_that_starts_with_the_prefix() => _reserved.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_refuse_the_names_the_bookkeeping_uses() => _bookkeeping.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_name_the_property_in_the_message() => _reserved.Message.ShouldContain("_mine");
    [Fact] void should_allow_any_other_name() => _allowed.ShouldBeNull();
    [Fact] void should_start_every_name_the_bookkeeping_uses_with_the_prefix() =>
        new[] { GraphBookkeeping.Watermark, GraphBookkeeping.Owner, GraphBookkeeping.Generation, GraphBookkeeping.Namespace, GraphBookkeeping.OwnerLabel }
            .All(_ => _.StartsWith(GraphBookkeeping.Prefix[0])).ShouldBeTrue();
}
