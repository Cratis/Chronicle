// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Identities.for_IdentityManager;

public class when_renaming_an_identity : given.an_identity_manager
{
    const string IdentitySubject = "person-42";
    const string NewName = "The New Name";

    async Task Because() => await _manager.Rename(IdentitySubject, NewName);

    [Fact]
    async Task should_rename_the_identity_in_the_store() =>
        await _identities.Received(1).RenameIdentity(
            Arg.Is<RenameIdentityRequest>(_ => _.EventStore == _eventStore.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_rename_the_identity_in_the_namespace() =>
        await _identities.Received(1).RenameIdentity(
            Arg.Is<RenameIdentityRequest>(_ => _.Namespace == _namespace.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_rename_the_identity_for_the_subject() =>
        await _identities.Received(1).RenameIdentity(
            Arg.Is<RenameIdentityRequest>(_ => _.Subject == IdentitySubject),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_rename_the_identity_to_the_new_name() =>
        await _identities.Received(1).RenameIdentity(
            Arg.Is<RenameIdentityRequest>(_ => _.Name == NewName),
            Arg.Any<CallContext>());
}
