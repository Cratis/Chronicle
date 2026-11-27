// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnations;

public class when_the_feature_is_disabled : given.incarnation_routing
{
    Exception _error;

    void Establish() => _routing = new(_storage, _grains, Options.Create(new ChronicleOptions()));

    async Task Because() => _error = await Catch.Exception(() => _routing.Ensure(_eventStore, _namespace));

    [Fact] void should_not_advertise_enablement() => _routing.Enabled.ShouldBeFalse();
    [Fact] void should_refuse_direct_coordination() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] async Task should_not_contact_the_owner() => await _owner.DidNotReceive().Ensure(_namespace);
}
