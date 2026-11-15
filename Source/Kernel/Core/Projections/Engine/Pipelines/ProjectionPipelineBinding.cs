// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>Associates a live pipeline with the compiled graph it actually owns.</summary>
/// <param name="Projection">The pipeline's compiled graph.</param>
/// <param name="Pipeline">The pipeline which owns buffered work for that graph.</param>
public record ProjectionPipelineBinding(IProjection Projection, IProjectionPipeline Pipeline)
{
    readonly object _gate = new();
    long _observedActivity = Pipeline.GetActivity();
    DateTimeOffset? _idleSince;

    /// <summary>Gets the definition fingerprint captured when the pipeline was created.</summary>
    public string Fingerprint { get; } = Projection.DefinitionFingerprint;

    /// <summary>Gets or sets whether the idle sweep released this binding.</summary>
    internal bool Swept { get; set; }

    /// <summary>
    /// Observes the pipeline and reports whether it has shown no activity since the previous observations for at least the timeout.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="timeout">How long the pipeline must have been inactive.</param>
    /// <returns>True when the pipeline has been idle for the timeout.</returns>
    internal bool IsIdle(DateTimeOffset now, TimeSpan timeout)
    {
        lock (_gate)
        {
            var activity = Pipeline.GetActivity();
            if (activity != _observedActivity || _idleSince is null)
            {
                _observedActivity = activity;
                _idleSince = now;
                return false;
            }

            return now - _idleSince.Value >= timeout;
        }
    }
}
