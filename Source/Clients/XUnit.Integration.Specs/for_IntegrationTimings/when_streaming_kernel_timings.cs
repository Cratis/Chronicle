// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_streaming_kernel_timings : Specification
{
    readonly List<string> _lines = [];

    void Because()
    {
        using var output = new IntegrationTimingOutput(_lines.Add);
        var first = Encoding.UTF8.GetBytes("not a timing: private application data\nCHRONICLE_INTEGRATION_TIM");
        output.Write(first, 0, first.Length);
        var second = Encoding.UTF8.GetBytes("ING {\"schema\":1}\r\nCHRONICLE_INTEGRATION_SUMMARY {\"schema\":1}\n");
        output.Write(second, 0, second.Length);
    }

    [Fact] void should_forward_only_diagnostic_lines() => _lines.Count.ShouldEqual(2);
    [Fact] void should_reassemble_split_lines() => _lines[0].ShouldEqual("CHRONICLE_INTEGRATION_TIMING {\"schema\":1}");
    [Fact] void should_preserve_the_summary() => _lines[1].ShouldEqual("CHRONICLE_INTEGRATION_SUMMARY {\"schema\":1}");
}
