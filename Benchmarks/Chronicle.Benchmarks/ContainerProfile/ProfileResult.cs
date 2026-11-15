// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A non-sensitive, per-repetition result; unavailable measurements remain null.</summary>
internal sealed class ProfileResult
{
    /// <summary>Gets the run identity.</summary>
    public required string RunId { get; init; }

    /// <summary>Gets the cell identity.</summary>
    public required string Cell { get; init; }

    /// <summary>Gets the source commit.</summary>
    public required string Sha { get; init; }

    /// <summary>Gets the repetition number.</summary>
    public required int Repetition { get; init; }

    /// <summary>Gets whether the OS page cache was warm.</summary>
    public bool WarmPageCache { get; } = true;

    /// <summary>Gets the scratch image reference.</summary>
    public required string ImageReference { get; init; }

    /// <summary>Gets the explicit GC overrides.</summary>
    public required string[] GcEnvironment { get; init; }

    /// <summary>Gets the workload scale.</summary>
    public required WorkloadSettings WorkloadSettings { get; init; }

    /// <summary>Gets whether tracing was enabled in this separate repetition.</summary>
    public required bool TraceRepetition { get; init; }

    /// <summary>Gets the explicit noisy-host override.</summary>
    public required bool AllowNoisyHost { get; init; }

    /// <summary>Gets the host-load ceiling.</summary>
    public required double MaxHostLoad { get; init; }

    /// <summary>Gets or sets completion, failure, deadline or OOM status.</summary>
    public string Status { get; set; } = "failed";

    /// <summary>Gets or sets the exception type, never its sensitive message.</summary>
    public string? FailureKind { get; set; }

    /// <summary>Gets or sets pre-run host conditions.</summary>
    public HostConditions? Host { get; set; }

    /// <summary>Gets or sets Docker creation wall time.</summary>
    public double? DockerCreateMs { get; set; }

    /// <summary>Gets or sets start-to-health readiness wall time.</summary>
    public double? ColdReadyMs { get; set; }

    /// <summary>Gets or sets graceful-stop wall time.</summary>
    public double? StopMs { get; set; }

    /// <summary>Gets or sets drain duration; unavailable without a kernel marker.</summary>
    public double? DrainMs { get; set; }

    /// <summary>Gets or sets completed workload measurements.</summary>
    public WorkloadResult? Workload { get; set; }

    /// <summary>Gets or sets pre-stop kernel state.</summary>
    public KernelState? StateBeforeStop { get; set; }

    /// <summary>Gets or sets post-stop exit and OOM state.</summary>
    public KernelState? StateAfterStop { get; set; }

    /// <summary>Gets or sets the idle sample after a 30 second settling period.</summary>
    public MemorySample? Idle { get; set; }

    /// <summary>Gets or sets separate trace GC measurements.</summary>
    public GcSummary? Gc { get; set; }

    /// <summary>Gets or sets a trace failure type without exposing diagnostics.</summary>
    public string? TraceFailureKind { get; set; }

    /// <summary>Gets timestamped process and cgroup memory samples.</summary>
    public List<MemorySample> Memory { get; } = [];

    /// <summary>Gets measurement boundaries and unavailable-metric semantics.</summary>
    public string Definitions { get; } = "Fresh kernel and MongoDB per run; Docker warm-page-cache start-return-to-/health/ready 200. RSS/HWM from kernel PID 1; cgroup usage from docker stats. Stop includes docker stop --time 60. Drain unavailable: no kernel drain marker. Missing metrics are null, never zero. Workbench assets absent. Client and MongoDB are unconstrained.";
}
