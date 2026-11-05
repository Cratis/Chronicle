// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts.given;

public class provisioned_namespaces : when_preparing_cycle_receipts.given.receipt_provisioning
{
    protected string _receiptDatabase;
    protected Exception _error;

    async Task Establish()
    {
        await _provisioner.Migrate(new(["orders", "shipping"], ["production", "staging"]));
        _receiptDatabase = Path.Combine(_directory, "cluster+shipping+staging.db");
    }

    protected Task Start(SchemaMode mode = SchemaMode.Verify) => new ClusterDbContextMigrator(CreateServices(mode).GetRequiredService<Database>()).Execute(CancellationToken.None);
}
