// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Removes Orleans grain ids before spans reach an exporter.
/// </summary>
public sealed class IdentifierSpanProcessor : BaseProcessor<Activity>
{
    /// <inheritdoc/>
    public override void OnEnd(Activity data)
    {
        data.SetTag("orleans.rpc.target_id", null)
            .SetTag("orleans.rpc.source_id", null);
    }
}
