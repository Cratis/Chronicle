// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ResourceNames;

public class when_checking_combinations : Specification
{
    ResourceNameViolation _lower;
    ResourceNameViolation _mixed;
    ResourceNameViolation _otherStore;
    ResourceNameViolation _otherNamespace;

    void Because()
    {
        _lower = ResourceNames.ForCombination("chronicle", "main");
        _mixed = ResourceNames.ForCombination("Chronicle", "MAIN");
        _otherStore = ResourceNames.ForCombination("orders", "main");
        _otherNamespace = ResourceNames.ForCombination("chronicle", "other");
    }

    [Fact] void should_refuse_chronicle_main() => _lower.ShouldEqual(ResourceNameViolation.ReservedCombination);
    [Fact] void should_refuse_chronicle_main_in_any_case() => _mixed.ShouldEqual(ResourceNameViolation.ReservedCombination);
    [Fact] void should_accept_main_in_other_store() => _otherStore.ShouldEqual(ResourceNameViolation.None);
    [Fact] void should_accept_other_namespace_in_chronicle() => _otherNamespace.ShouldEqual(ResourceNameViolation.None);
}
