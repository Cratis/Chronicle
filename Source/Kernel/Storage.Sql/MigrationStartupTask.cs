// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents a migration startup task for handling database migrations.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
public class MigrationStartupTask(IServiceProvider serviceProvider) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(nameof(MigrationStartupTask), ServiceLifecycleStage.First, Execute);
    }

    private async Task Execute(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        foreach (var dbContextType in scope.ServiceProvider.GetRequiredService<IImplementationsOf<BaseDbContext>>().OnlyChronicle())
        {
            var dbContext = (scope.ServiceProvider.GetRequiredService(dbContextType) as DbContext)!;
            await dbContext.Database.MigrateAsync();
        }
    }
}
