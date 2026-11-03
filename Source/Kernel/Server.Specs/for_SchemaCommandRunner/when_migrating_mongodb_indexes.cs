// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Schema;

namespace Cratis.Chronicle.Server.for_SchemaCommandRunner;

public class when_migrating_mongodb_indexes : Specification
{
    IMongoDBIndexProvisioner _provisioner;
    SchemaCommand _command;
    int _exit;

    void Establish()
    {
        _command = SchemaCommand.Parse(["--migrate-only", "--event-store", "orders"]);
        _provisioner = Substitute.For<IMongoDBIndexProvisioner>();
        _provisioner.Migrate(_command.Scope.EventStores, _command.Scope.Namespaces).Returns(new MongoDBIndexReport([new("namespace:orders/default", "orders_default", [], [])]));
    }

    async Task Because() => _exit = await SchemaCommandRunner.RunMongoAction(_provisioner, _command, SchemaAction.MigrateAndExit);

    [Fact] void should_return_success_when_provisioned() => _exit.ShouldEqual(0);
    [Fact] void should_pass_explicit_scope_to_migrate() => _provisioner.Received().Migrate(_command.Scope.EventStores, _command.Scope.Namespaces);
    [Fact] void should_not_run_a_separate_verify_action() => _provisioner.DidNotReceive().Verify(Arg.Any<IEnumerable<string>?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>());
}
