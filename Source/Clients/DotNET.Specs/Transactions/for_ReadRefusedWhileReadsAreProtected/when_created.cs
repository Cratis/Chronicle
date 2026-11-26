// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_ReadRefusedWhileReadsAreProtected;

public class when_created : Specification
{
    const string Operation = "IReadModels.GetInstanceById";

    ReadRefusedWhileReadsAreProtected _exception;

    void Because() => _exception = new ReadRefusedWhileReadsAreProtected(Operation);

    [Fact] void should_name_the_operation() => _exception.Message.ShouldContain(Operation);
    [Fact] void should_recommend_decision_reads() => _exception.Message.ShouldContain("IDecisionReads");
    [Fact] void should_point_at_handle() => _exception.Message.ShouldContain("Handle()");
    [Fact] void should_not_mention_provide() => _exception.Message.ShouldNotContain("Provide");
    [Fact] void should_offer_the_unprotected_hint() => _exception.Message.ShouldContain("[Unprotected");
}
