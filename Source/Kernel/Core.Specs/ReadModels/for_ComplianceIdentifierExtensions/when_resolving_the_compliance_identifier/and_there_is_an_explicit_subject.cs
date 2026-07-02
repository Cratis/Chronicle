// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.when_resolving_the_compliance_identifier;

public class and_there_is_an_explicit_subject : given.an_event_context
{
    string _result;

    void Because()
    {
        // The document is re-keyed, yet an explicit subject that differs from the event source id still wins.
        _result = EventContextFor(EventSourceIdValue, (Subject)ExplicitSubjectValue).ResolveComplianceIdentifier(KeyFor(ResolvedKeyValue));
    }

    [Fact] void should_use_the_explicit_subject() => _result.ShouldEqual(ExplicitSubjectValue);
}
