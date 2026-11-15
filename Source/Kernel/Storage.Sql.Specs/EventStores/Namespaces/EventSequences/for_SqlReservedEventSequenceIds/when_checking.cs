// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_SqlReservedEventSequenceIds;

public class when_checking : Specification
{
    readonly SqlReservedEventSequenceIds _reserved = new();

    bool IsReserved(string id) => _reserved.IsReserved(new Concepts.EventSequences.EventSequenceId(id));

    [Theory]
    [InlineData("Observers")]
    [InlineData("observers")]
    [InlineData("EventSequences")]
    [InlineData("JobSteps")]
    [InlineData("__cratis_named_tags")]
    [InlineData("__EFMigrationsHistory")]
    [InlineData("orders_email_constraint")]
    [InlineData("orders_email_constraint_values")]
    void should_reserve(string id) => IsReserved(id).ShouldBeTrue();

    [Theory]
    [InlineData("orders")]
    [InlineData("order_lines")]
    [InlineData("orders-changes")]
    void should_not_reserve(string id) => IsReserved(id).ShouldBeFalse();
}
