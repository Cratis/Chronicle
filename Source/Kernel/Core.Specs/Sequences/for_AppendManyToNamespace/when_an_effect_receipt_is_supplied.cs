// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyToNamespace;

public class when_an_effect_receipt_is_supplied : given.a_protected_batch_endpoint
{
    Occurrences.EffectReceipt _expectedReceipt;

    void Establish()
    {
        _expectedReceipt = new("occurrence", "effect", 1);
        _command = _command with { EffectReceipt = _expectedReceipt };
    }

    Task Because() => Append();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_forward_the_receipt() => _receipt.ShouldEqual(_expectedReceipt);
    [Fact] void should_only_use_the_protected_receipt_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendManyWithEffectReceiptToNamespace");
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}
