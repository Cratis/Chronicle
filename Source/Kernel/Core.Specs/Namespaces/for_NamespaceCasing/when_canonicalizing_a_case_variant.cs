// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_canonicalizing_a_case_variant : given.a_namespace_casing
{
    void Establish() => _namespaces.GetCanonical("cratis").Returns((EventStoreNamespaceName)"Cratis");

    async Task Because()
    {
        _first = await _casing.Canonicalize("Studio", "cratis");
        _second = await _casing.Canonicalize("Studio", "cratis");
    }

    [Fact] void should_return_the_stored_casing() => _first.Value.ShouldEqual("Cratis");
    [Fact] void should_keep_the_cached_casing() => _second.ShouldEqual(_first);
    [Fact] async Task should_query_the_grain_once() => await _namespaces.Received(1).GetCanonical("cratis");
    [Fact]
    void should_log_the_two_forms_once_at_information() => _logger.Entries.ShouldContainOnly(
        (LogLevel.Information, "Namespace 'cratis' in event store 'Studio' is canonicalized to 'Cratis'"));
}
