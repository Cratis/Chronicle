// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Applies identifier exposure rules to tracing tags supplied by Orleans.
/// </summary>
/// <param name="options">The Chronicle configuration.</param>
public sealed class IdentifierSpanProcessor(IOptions<ChronicleOptions> options) : BaseProcessor<Activity>
{
    readonly IdentifierTelemetry _identifiers = new(options.Value.Telemetry);

    /// <inheritdoc/>
    public override void OnEnd(Activity data)
    {
        if (_identifiers.SpanExposure == IdentifierExposure.Raw) return;

        RewriteGrainId(data, "orleans.rpc.target_id");
        RewriteGrainId(data, "orleans.rpc.source_id");
        if (_identifiers.SpanExposure == IdentifierExposure.Omit) data.SetTag("cratis.eventsource.id", null);
    }

    void RewriteGrainId(Activity activity, string tag)
    {
        if (activity.GetTagItem(tag) is not string value) return;
        if (_identifiers.SpanExposure == IdentifierExposure.Omit)
        {
            activity.SetTag(tag, null);
            return;
        }

        var separator = value.IndexOf('/');
        var rewritten = separator < 0
            ? _identifiers.ForSpan(value)
            : $"{value[..(separator + 1)]}{_identifiers.ForSpan(value[(separator + 1)..])}";
        activity.SetTag(tag, rewritten);
    }
}
