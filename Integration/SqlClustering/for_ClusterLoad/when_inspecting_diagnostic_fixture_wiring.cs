// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_inspecting_diagnostic_fixture_wiring : Specification
{
    bool _enabled;
    bool _hasCapture;
    bool _hasLoggerFactory;
    bool _hasDiagnosticTeardown;
    long _constructorBytes;

    async Task Because()
    {
        _enabled = typeof(ChronicleClient).Assembly.GetType("Cratis.Chronicle.Reactors.ReactorsDeliveryDiagnosticsLogging") is not null;
        var type = typeof(ClusterLoad);
        _hasCapture = type.GetField("_deliveryDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic) is not null;
        _hasLoggerFactory = type.GetField("_diagnosticLoggerFactory", BindingFlags.Instance | BindingFlags.NonPublic) is not null;
        _hasDiagnosticTeardown = type.GetMethod("DisposeDeliveryDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic) is not null;
        var before = GC.GetAllocatedBytesForCurrentThread();
        await using var fixture = new ClusterLoad();
        _constructorBytes = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] public void should_compile_capture_ownership_only_into_the_candidate() => _hasCapture.ShouldEqual(_enabled);
    [Fact] public void should_compile_logger_ownership_only_into_the_candidate() => _hasLoggerFactory.ShouldEqual(_enabled);
    [Fact] public void should_compile_diagnostic_teardown_only_into_the_candidate() => _hasDiagnosticTeardown.ShouldEqual(_enabled);
    [Fact] public void should_not_allocate_capture_storage_in_the_normal_fixture() => (_enabled || _constructorBytes < 1024 * 1024).ShouldBeTrue();
}
