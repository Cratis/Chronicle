// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Lists read model instances with pagination.
/// </summary>
public class GetReadModelInstancesCommand : ChronicleCommand<GetReadModelInstancesSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, GetReadModelInstancesSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var response = await services.ReadModels.GetInstances(new GetInstancesRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ReadModel = settings.ReadModel,
            Page = settings.Page,
            PageSize = settings.PageSize
        });

        OutputFormatter.WriteObject(
            format,
            new
            {
                response.TotalCount,
                response.Page,
                response.PageSize,
                Instances = (response.Instances ?? []).ToList()
            },
            data =>
            {
                Console.WriteLine($"Total: {data.TotalCount} | Page: {data.Page} | PageSize: {data.PageSize}");
                Console.WriteLine();
                foreach (var instance in data.Instances)
                {
                    Console.WriteLine(instance);
                }
            });

        return ExitCodes.Success;
    }
}
