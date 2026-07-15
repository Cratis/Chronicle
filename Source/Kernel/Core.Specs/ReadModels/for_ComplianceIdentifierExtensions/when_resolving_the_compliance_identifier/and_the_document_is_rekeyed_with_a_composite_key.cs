// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.when_resolving_the_compliance_identifier;

public class and_the_document_is_rekeyed_with_a_composite_key : given.an_event_context
{
    Key _key;
    string _result;

    void Establish()
    {
        dynamic composite = new ExpandoObject();
        composite.provider = "github";
        composite.subject = "134365";
        _key = new Key(composite, ArrayIndexers.NoIndexers);
    }

    void Because() => _result = EventContextFor(EventSourceIdValue).ResolveComplianceIdentifier(_key);

    [Fact] void should_combine_the_key_properties_into_a_stable_identifier() => _result.ShouldEqual("provider=github+subject=134365");
    [Fact] void should_not_use_the_expando_object_type_name() => _result.ShouldNotEqual(typeof(ExpandoObject).ToString());
    [Fact] void should_not_use_the_event_source_id() => _result.ShouldNotEqual(EventSourceIdValue);
}
