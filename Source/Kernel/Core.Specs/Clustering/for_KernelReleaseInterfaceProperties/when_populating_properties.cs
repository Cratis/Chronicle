// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Clustering.for_KernelReleaseInterfaceProperties;

public class when_populating_properties : Specification
{
    readonly Dictionary<string, string> _observer = [];
    readonly Dictionary<string, string> _other = [];
    void Because()
    {
        var provider = new KernelReleaseInterfaceProperties();
        provider.Populate(typeof(IObserver), default, _observer);
        provider.Populate(typeof(IPatchManager), default, _other);
    }
    [Fact] void should_stamp_the_existing_observer_interface() => _observer[KernelReleaseInterfaceProperties.ReleaseKey].ShouldEqual(KernelVersion.Current.ToString());
    [Fact] void should_not_stamp_other_interfaces() => _other.ShouldBeEmpty();
}
