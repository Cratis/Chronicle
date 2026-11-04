// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_callbacks_repeat_across_incarnations : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    readonly Guid _id = Guid.NewGuid();
    readonly Guid _first = Guid.NewGuid();
    readonly Guid _last = Guid.NewGuid();
    string _report = null!;
    int _enqueues;

    void Because()
    {
        using (_diagnostics.BeginScope(_first, 1))
        {
            _diagnostics.Callback(_id, 9, () => _enqueues++);
        }
        using (_diagnostics.BeginScope(_last, 2))
        {
            _diagnostics.Callback(_id, 9, () => _enqueues++);
        }
        _report = _diagnostics.Describe([(_id, 9UL)], []);
    }

    void Destroy() => _diagnostics.Dispose();

    [Fact] public void should_preserve_every_original_enqueue() => _enqueues.ShouldEqual(2);
    [Fact] public void should_record_all_success_stages() => _report.Contains("stages=Entry, Enqueued, Exit entries=2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_retain_the_last_exact_dispatch_scope() => _report.Contains($"last-stream={_last} last-batch=2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_not_claim_membership_from_callback_only_capture() => _report.Contains("coverage=unknown", StringComparison.Ordinal).ShouldBeTrue();
}
