// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_PatternDetection;

public class when_asking_whether_capture_is_enabled : Specification
{
    bool _byDefault;
    bool _whenSwitchedOff;
    bool _storeOffNamespaceOn;
    bool _storeOnNamespaceOff;
    bool _otherStore;
    bool _withDifferentCasing;

    void Because()
    {
        _byDefault = new PatternDetection().IsEnabledFor("store", "namespace");
        _whenSwitchedOff = new PatternDetection { Enabled = false }.IsEnabledFor("store", "namespace");

        var storeOff = new PatternDetection();
        storeOff.Overrides["store"] = false;
        storeOff.Overrides["store/live"] = true;
        _storeOffNamespaceOn = storeOff.IsEnabledFor("store", "live");
        _otherStore = storeOff.IsEnabledFor("another-store", "live");
        _withDifferentCasing = storeOff.IsEnabledFor("STORE", "other");

        var namespaceOff = new PatternDetection { Enabled = false };
        namespaceOff.Overrides["store"] = true;
        namespaceOff.Overrides["store/quiet"] = false;
        _storeOnNamespaceOff = namespaceOff.IsEnabledFor("store", "quiet");
    }

    [Fact] void should_be_on_by_default() => _byDefault.ShouldBeTrue();
    [Fact] void should_be_off_when_switched_off() => _whenSwitchedOff.ShouldBeFalse();
    [Fact] void should_let_a_namespace_entry_win_over_the_event_store_entry_when_on() => _storeOffNamespaceOn.ShouldBeTrue();
    [Fact] void should_let_a_namespace_entry_win_over_the_event_store_entry_when_off() => _storeOnNamespaceOff.ShouldBeFalse();
    [Fact] void should_not_let_an_event_store_entry_reach_another_event_store() => _otherStore.ShouldBeTrue();
    [Fact] void should_not_care_about_casing() => _withDifferentCasing.ShouldBeFalse();
}
