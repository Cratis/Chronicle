// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Gets snapshots for a read model instance by key.
/// </summary>
public class GetReadModelSnapshotsCommand : ChronicleCommand<ReadModelKeySettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ReadModelKeySettings settings, string format)
    {
        var response = await services.ReadModels.GetSnapshotsByKey(new GetSnapshotsByKeyRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ReadModelIdentifier = settings.ReadModel,
            EventSequenceId = settings.EventSequenceId,
            ReadModelKey = settings.Key
        });

        var snapshots = (response.Snapshots ?? []).ToList();

        if (snapshots.Count == 0)
        {
            OutputFormatter.WriteMessage(format, $"No snapshots found for key '{settings.Key}' in read model '{settings.ReadModel}'");
            return ExitCodes.Success;
        }

        OutputFormatter.Write(
            format,
            snapshots,
            ["Occurred", "CorrelationId", "Events", "ReadModel"],
            snap =>
            [
                snap.Occurred?.ToString() ?? string.Empty,
                snap.CorrelationId.ToString(),
                snap.Events.Count.ToString(),
                snap.ReadModel.Length > 80 ? snap.ReadModel[..80] + "..." : snap.ReadModel
            ]);

        return ExitCodes.Success;
    }
}
