// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.when_resolving_the_compliance_identifier;

public class and_the_document_is_rekeyed : given.an_event_context
{
    string _result;

    void Because() => _result = EventContextFor(EventSourceIdValue).ResolveComplianceIdentifier(KeyFor(ResolvedKeyValue));

    [Fact] void should_use_the_resolved_document_key() => _result.ShouldEqual(ResolvedKeyValue);
    [Fact] void should_not_use_the_event_source_id() => _result.ShouldNotEqual(EventSourceIdValue);
}
