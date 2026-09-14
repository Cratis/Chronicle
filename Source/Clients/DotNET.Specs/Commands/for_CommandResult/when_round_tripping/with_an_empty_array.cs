// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf;

namespace Cratis.Chronicle.Commands.for_CommandResult.when_round_tripping;

public class with_an_empty_array : Specification
{
    CommandResult<string[]> _result;

    void Because() => _result = Serializer.DeepClone(CommandResult<string[]>.Success(Guid.Empty, []));

    [Fact] void should_stay_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_supply_an_empty_array() => _result.Response.ShouldBeEmpty();
}
