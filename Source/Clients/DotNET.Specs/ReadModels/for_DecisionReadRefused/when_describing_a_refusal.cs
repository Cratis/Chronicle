// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReadRefused;

public class when_describing_a_refusal
{
    public static TheoryData<DecisionReadRefusalReason> Reasons => [.. Enum.GetValues<DecisionReadRefusalReason>()];

    [Theory]
    [MemberData(nameof(Reasons))]
    public void should_explain_every_reason(DecisionReadRefusalReason reason) =>
        new DecisionReadRefused(reason, typeof(Model)).Message.Length.ShouldBeGreaterThan($"Decision read of '{typeof(Model)}' was refused: {reason}.".Length);

    [Fact]
    public void should_say_what_to_do_about_a_hierarchy() =>
        new DecisionReadRefused(DecisionReadRefusalReason.Hierarchy, typeof(Model)).Message.ShouldEqual(
            $"Decision read of '{typeof(Model)}' was refused: Hierarchy. The projection has children or nested objects, and a decision read only folds flat projections. " +
            "Read a flat read model, for example a dedicated [Passive] one, instead.");

    record Model;
}
