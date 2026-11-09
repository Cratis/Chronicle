// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_forgetting_a_cached_namespace : given.a_namespace_casing
{
    void Establish() => _namespaces.GetCanonical("cratis").Returns((EventStoreNamespaceName)"Cratis", (EventStoreNamespaceName)"cratis", (EventStoreNamespaceName)"Cratis");

    async Task Because()
    {
        _first = await _casing.Canonicalize("Studio", "cratis");
        _casing.Forget("Studio", "CRATIS");
        _second = await _casing.Canonicalize("Studio", "cratis");
        _casing.Forget("Studio", "cratis");
        await _casing.Canonicalize("Studio", "cratis");
    }

    [Fact] void should_return_the_original_casing_before_removal() => _first.Value.ShouldEqual("Cratis");
    [Fact] void should_resolve_the_new_casing_after_removal() => _second.Value.ShouldEqual("cratis");
    [Fact] async Task should_resolve_the_forgotten_name_again() => await _namespaces.Received(3).GetCanonical("cratis");
    [Fact] void should_log_the_variant_again_after_removal() => _logger.Entries.Count(_ => _.Level == LogLevel.Information && _.Message.Contains("'cratis'", StringComparison.Ordinal)).ShouldEqual(2);
}
