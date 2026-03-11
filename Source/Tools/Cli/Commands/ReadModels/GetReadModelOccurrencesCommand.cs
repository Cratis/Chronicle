// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Lists read model occurrences (replay history).
/// </summary>
public class GetReadModelOccurrencesCommand : ChronicleCommand<GetReadModelOccurrencesSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, GetReadModelOccurrencesSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var response = await services.ReadModels.GetOccurrences(new GetOccurrencesRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            Type = new ReadModelType
            {
                Identifier = settings.ReadModelType,
                Generation = settings.Generation
            }
        });

        var list = (response.Occurrences ?? []).ToList();

        OutputFormatter.Write(
            format,
            list,
            ["ObserverId", "Occurred", "Type", "Container", "RevertContainer"],
            occ =>
            [
                occ.ObserverId,
                occ.Occurred?.ToString() ?? string.Empty,
                $"{occ.Type?.Identifier}+{occ.Type?.Generation}",
                occ.ContainerName,
                occ.RevertContainerName
            ]);

        return ExitCodes.Success;
    }
}
