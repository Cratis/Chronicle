// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.when_resolving_the_compliance_identifier;

public class and_there_is_no_explicit_subject : given.an_event_context
{
    string _result;

    void Because() => _result = EventContextFor(EventSourceIdValue).ResolveComplianceIdentifier(KeyFor(EventSourceIdValue));

    [Fact] void should_use_the_document_key() => _result.ShouldEqual(EventSourceIdValue);
}
