// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts.given;

public class provisioned_namespaces : when_preparing_cycle_receipts.given.receipt_provisioning
{
    protected string _receiptDatabase;
    protected Exception _error;
    protected ResourceNameSnapshot _resourceNames;
    protected ReadModelSchemaVerification _verification;

    async Task Establish()
    {
        await _provisioner.Migrate(new(["orders", "shipping"], ["production", "staging"]));
        _receiptDatabase = Path.Combine(_directory, "cluster+shipping+staging.db");
        _resourceNames = new ResourceNameSnapshot();
    }

    protected async Task Start(SchemaMode mode = SchemaMode.Verify)
    {
        var database = CreateServices(mode).GetRequiredService<Database>();
        await new ClusterDbContextMigrator(database).Execute(CancellationToken.None);
        _verification = new ReadModelSchemaVerification(database, NullLogger<ReadModelSchemaVerification>.Instance, _resourceNames);
        await _verification.Execute(CancellationToken.None);
    }
}
