// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationExecutionSpecification;

public class when_validating_captured_content : Specification
{
    MaterializationExecutionSpecification _execution;
    Exception _error;
    void Establish() => _execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"a\":{\"x\":1,\"y\":2},\"z\":[{\"a\":1,\"b\":2},3]}");
    void Because() => _error = Catch.Exception(_execution.EnsureValid);
    [Fact] void should_accept_its_content() => _error.ShouldBeNull();
    [Fact] void should_match_the_version_one_golden_digest() => _execution.Revision.Value.ShouldEqual("v1:20d71305c2813d2a9adbe5856c1c0dbf3b43efdcd9592e8b3de44188129b77f5");
    [Fact] void should_refuse_changed_content() => Catch.Exception((_execution with { CanonicalContent = "{}" }).EnsureValid).ShouldBeOfExactType<InvalidMaterializationExecution>();
    [Fact] void should_refuse_unknown_interpretation() => Catch.Exception((_execution with { InterpretationVersion = 2 }).EnsureValid).ShouldBeOfExactType<InvalidMaterializationExecution>();
}
