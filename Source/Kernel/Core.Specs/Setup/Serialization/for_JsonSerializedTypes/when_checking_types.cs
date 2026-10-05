// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Setup.Serialization.for_JsonSerializedTypes;

public class when_checking_types : Specification
{
    [Fact] void should_exclude_cratis_exceptions() => JsonSerializedTypes.Includes(typeof(ClientIsNotConnected)).ShouldBeFalse();
    [Fact] void should_exclude_system_exceptions() => JsonSerializedTypes.Includes(typeof(InvalidOperationException)).ShouldBeFalse();
    [Fact] void should_include_other_cratis_types() => JsonSerializedTypes.Includes(typeof(ConnectionId)).ShouldBeTrue();
    [Fact] void should_exclude_unrelated_types() => JsonSerializedTypes.Includes(typeof(Uri)).ShouldBeFalse();
}
