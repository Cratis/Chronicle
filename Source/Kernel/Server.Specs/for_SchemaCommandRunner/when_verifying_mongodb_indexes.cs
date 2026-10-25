// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Schema;

namespace Cratis.Chronicle.Server.for_SchemaCommandRunner;

public class when_verifying_mongodb_indexes : Specification
{
    IMongoDBIndexProvisioner _provisioner;
    SchemaCommand _command;
    int _exit;

    void Establish()
    {
        _command = SchemaCommand.Parse(["--verify-schema", "--event-store", "orders", "--namespace", "tenant"]);
        _provisioner = Substitute.For<IMongoDBIndexProvisioner>();
        _provisioner.Verify(_command.Scope.EventStores, _command.Scope.Namespaces).Returns(new MongoDBIndexReport([new("namespace:orders/tenant", "orders_tenant", ["event-log/subject_index"], [])]));
    }

    async Task Because() => _exit = await SchemaCommandRunner.RunMongoAction(_provisioner, _command, SchemaAction.VerifyAndExit);

    [Fact] void should_return_two_for_unprovisioned_indexes() => _exit.ShouldEqual(2);
    [Fact] void should_pass_explicit_scope_to_verify() => _provisioner.Received().Verify(_command.Scope.EventStores, _command.Scope.Namespaces);
    [Fact] void should_never_migrate_during_verify() => _provisioner.DidNotReceive().Migrate(Arg.Any<IEnumerable<string>?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>());
}
