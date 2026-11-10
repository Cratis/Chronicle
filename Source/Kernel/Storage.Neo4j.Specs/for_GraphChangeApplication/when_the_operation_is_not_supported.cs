// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphChangeApplication;

public class when_the_operation_is_not_supported : Specification
{
    IAsyncQueryRunner _transaction;
    Exception _error;

    void Establish() => _transaction = Substitute.For<IAsyncQueryRunner>();

    async Task Because()
    {
        var application = new GraphChangeApplication(GraphDefinitions.Organization, new(GraphDefinitions.Organization, null), _transaction, "owner", 1, 0, null);
        _error = await Catch.Exception(() => application.Apply(new UnknownOperation()));
    }

    [Fact] void should_refuse_the_operation() => _error.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_name_the_operation() => _error.Message.ShouldContain(nameof(UnknownOperation));
    [Fact] void should_not_touch_the_database() => _transaction.ReceivedCalls().ShouldBeEmpty();

    sealed record UnknownOperation : GraphOperation;
}
