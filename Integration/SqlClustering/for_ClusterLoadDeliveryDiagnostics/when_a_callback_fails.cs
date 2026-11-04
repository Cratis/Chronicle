// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_a_callback_fails : Specification
{
    readonly ClusterLoadDeliveryDiagnostics _diagnostics = new();
    readonly Guid _id = Guid.NewGuid();
    readonly InvalidOperationException _original = new("must not be formatted");
    Exception _caught = null!;
    string _report = null!;
    int _calls;

    void Because()
    {
        try
        {
            _diagnostics.Callback(_id, 7, () =>
            {
                _calls++;
                throw _original;
            });
        }
        catch (Exception ex)
        {
            _caught = ex;
        }
        _report = _diagnostics.Describe([(_id, 7UL)], []);
    }

    void Destroy() => _diagnostics.Dispose();

    [Fact] public void should_preserve_the_original_exception() => _caught.ShouldEqual(_original);
    [Fact] public void should_call_the_original_enqueue_once() => _calls.ShouldEqual(1);
    [Fact] public void should_record_entry_and_fault_but_not_enqueue_or_exit() => _report.Contains("stages=Entry, Fault", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] public void should_not_format_exception_details() => _report.Contains(_original.Message, StringComparison.Ordinal).ShouldBeFalse();
}
