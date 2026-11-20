// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_DurabilityClassifier;

public class when_comparing_results : Specification
{
    bool _same;
    bool _different = true;
    bool _single = true;
    bool _unknown = true;
    string _label = string.Empty;

    void Because()
    {
        _same = DurabilityClassifier.AreComparable([DurabilityClassifier.SyncOnCommit, DurabilityClassifier.SyncOnCommit]);
        _different = DurabilityClassifier.AreComparable([DurabilityClassifier.SyncOnCommit, DurabilityClassifier.DeferredFlush]);
        _single = DurabilityClassifier.AreComparable([DurabilityClassifier.SyncOnCommit]);
        _unknown = DurabilityClassifier.AreComparable([DurabilityClassifier.Unknown, DurabilityClassifier.Unknown]);
        _label = DurabilityClassifier.ComparabilityLabel([DurabilityClassifier.SyncOnCommit, DurabilityClassifier.NoFlush]);
    }

    [Fact] void should_compare_equal_classes() => _same.ShouldBeTrue();
    [Fact] void should_not_compare_different_classes() => _different.ShouldBeFalse();
    [Fact] void should_not_compare_a_single_result() => _single.ShouldBeFalse();
    [Fact] void should_not_compare_unknown_classes() => _unknown.ShouldBeFalse();
    [Fact] void should_label_the_tradeoff() => _label.ShouldEqual("different tradeoff");
}
