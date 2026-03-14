// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Cli.Commands.Projections;

/// <summary>
/// Lists projection definitions in an event store.
/// </summary>
public class ListProjectionsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var definitions = await services.Projections.GetAllDefinitions(new GetAllDefinitionsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var list = definitions.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Identifier", "ReadModel", "Active", "Rewindable", "AutoMap"],
            def =>
            [
                def.Identifier,
                def.ReadModel,
                def.IsActive.ToString(),
                def.IsRewindable.ToString(),
                def.AutoMap.ToString()
            ]);

        return ExitCodes.Success;
    }
}
