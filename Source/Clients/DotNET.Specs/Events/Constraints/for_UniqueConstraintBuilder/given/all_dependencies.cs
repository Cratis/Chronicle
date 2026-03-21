// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.given;

public class all_dependencies : Specification
{
    protected IEventTypes _eventTypes;
    protected JsonSchemaGenerator _generator;
    protected INamingPolicy _namingPolicy;

    void Establish()
    {
        _namingPolicy = new DefaultNamingPolicy();
        _eventTypes = Substitute.For<IEventTypes>();
        _generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            _namingPolicy);
    }
}
