// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_response_defaults;

public class when_populating_collection_payloads : Specification
{
    QueryResult<IEnumerable<string>> _query;
    CommandResult<IEnumerable<string>> _command;
    QueryResult<IEnumerable<string>> _otherQuery;
    CommandResult<IEnumerable<string>> _otherCommand;

    void Because()
    {
        _query = new();
        _command = new();
        _otherQuery = new();
        _otherCommand = new();
        ((ICollection<string>)_query.Data).Add("query value");
        ((ICollection<string>)_command.Response).Add("command value");
        _query = Serializer.DeepClone(_query);
        _command = Serializer.DeepClone(_command);
    }

    [Fact] void should_preserve_query_values_on_the_wire() => _query.Data.ShouldContainOnly("query value");
    [Fact] void should_preserve_command_values_on_the_wire() => _command.Response.ShouldContainOnly("command value");
    [Fact] void should_not_share_query_collections() => _otherQuery.Data.ShouldBeEmpty();
    [Fact] void should_not_share_command_collections() => _otherCommand.Response.ShouldBeEmpty();
}
