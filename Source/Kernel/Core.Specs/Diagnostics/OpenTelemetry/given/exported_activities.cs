// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.given;

public class exported_activities(ICollection<Activity> activities) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch) activities.Add(activity);
        return ExportResult.Success;
    }
}
