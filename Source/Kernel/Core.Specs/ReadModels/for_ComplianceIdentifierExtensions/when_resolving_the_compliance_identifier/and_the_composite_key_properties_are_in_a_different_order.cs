// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.ReadModels.for_ComplianceIdentifierExtensions.when_resolving_the_compliance_identifier;

public class and_the_composite_key_properties_are_in_a_different_order : given.an_event_context
{
    Key _key;
    string _result;

    void Establish()
    {
        dynamic composite = new ExpandoObject();
        composite.subject = "134365";
        composite.provider = "github";
        _key = new Key(composite, ArrayIndexers.NoIndexers);
    }

    void Because() => _result = EventContextFor(EventSourceIdValue).ResolveComplianceIdentifier(_key);

    [Fact] void should_produce_the_same_identifier_regardless_of_property_order() => _result.ShouldEqual("provider=github+subject=134365");
}
