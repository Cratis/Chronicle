// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_NativeSignals;

/// <summary>
/// Specifies native ABI mappings without delivering signals to any process.
/// </summary>
public class when_mapping_signal_numbers : Specification
{
    [Fact] void should_map_darwin_stop() => NativeSignals.NumberFor("STOP", macOS: true).ShouldEqual(17);
    [Fact] void should_map_darwin_continue() => NativeSignals.NumberFor("CONT", macOS: true).ShouldEqual(19);
    [Fact] void should_map_linux_stop() => NativeSignals.NumberFor("STOP", macOS: false).ShouldEqual(19);
    [Fact] void should_map_linux_continue() => NativeSignals.NumberFor("CONT", macOS: false).ShouldEqual(18);
    [Fact] void should_reject_unsupported_signals() => Assert.Throws<ArgumentOutOfRangeException>(() => NativeSignals.NumberFor("KILL", macOS: false));
    [Fact] void should_never_signal_a_process_group() => Assert.Throws<ArgumentOutOfRangeException>(() => NativeSignals.Send(0, "STOP"));
}
