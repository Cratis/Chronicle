// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_ReportRenderer.when_rendering_workflow_commands;

public class and_payload_and_grain_breaks_are_reported : Specification
{
    string _payload;
    string _grain;

    void Because()
    {
        _payload = ReportRenderer.ToWorkflowCommands("release", [new(ContractBreakKind.PayloadPropertyRemoved, "Payload", "Removed\nproperty")]);
        _grain = ReportRenderer.ToWorkflowCommands("release", [new(ContractBreakKind.MethodRemoved, "Grain", "Removed method")]);
    }

    [Fact] void should_identify_the_payload_snapshot() => _payload.ShouldContain("file=Source/Kernel/Core/GrainPayloadContracts.txt,title=Payload contract break");
    [Fact] void should_preserve_grain_annotations() => _grain.ShouldContain("file=Source/Kernel/Core/OrleansContracts.txt,title=Grain contract break");
    [Fact] void should_keep_escaping_payload_descriptions() => _payload.ShouldContain("Removed%0Aproperty");
}
