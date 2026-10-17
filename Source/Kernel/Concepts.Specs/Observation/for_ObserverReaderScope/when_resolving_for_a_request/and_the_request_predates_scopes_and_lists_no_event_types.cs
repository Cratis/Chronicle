// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_resolving_for_a_request;

public class and_the_request_predates_scopes_and_lists_no_event_types : Specification
{
    ObserverReaderScope _result;

    void Because() => _result = ObserverReaderScope.ForRequest(null, []);

    [Fact] void should_be_a_wildcard() => _result.IsWildcard.ShouldBeTrue();
}
