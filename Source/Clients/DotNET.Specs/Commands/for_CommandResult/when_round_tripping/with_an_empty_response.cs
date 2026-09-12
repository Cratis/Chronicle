// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf;

namespace Cratis.Chronicle.Commands.for_CommandResult.when_round_tripping;

public class with_an_empty_response : Specification
{
    CommandResult<AppendResponse> _result;

    void Because()
    {
        var before = new CommandResult<AppendResponse>();
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, before);
        stream.Position = 0;
        _result = Serializer.Deserialize<CommandResult<AppendResponse>>(stream);
    }

    [Fact] void should_supply_a_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_supply_empty_constraint_violations() => _result.Response.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_supply_empty_errors() => _result.Response.Errors.ShouldBeEmpty();
}
