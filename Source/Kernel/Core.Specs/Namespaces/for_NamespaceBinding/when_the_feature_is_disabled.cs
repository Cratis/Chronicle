// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_feature_is_disabled : given.a_binding_lookup
{
    Exception _error;

    void Establish() => _routing = new(_storage, _grains, Options.Create(new ChronicleOptions()));

    async Task Because() => _error = await Catch.Exception(Lookup);

    [Fact] void should_refuse_binding() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] void should_not_read_or_change_control_state() => _lifecycle.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_a_namespace() => _names.ReceivedCalls().ShouldBeEmpty();
}
