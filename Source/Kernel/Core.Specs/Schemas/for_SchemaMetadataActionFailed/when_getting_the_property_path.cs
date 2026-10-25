// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SchemaMetadataActionFailed;

public class when_getting_the_property_path : Specification
{
    SchemaMetadataActionFailed _error;
    Exception _cause;

    void Establish() => _cause = new InvalidOperationException("private value");

    void Because() => _error = new SchemaMetadataActionFailed(SchemaMetadataActionFailed.ReleaseAction, "contacts[0].name", "subject", _cause);

    [Fact] void should_expose_only_the_path() => _error.PropertyPath.ShouldEqual("contacts[0].name");
    [Fact] void should_preserve_the_message() => _error.Message.ShouldEqual("Failed to release schema metadata for property 'contacts[0].name' of 'subject'.");
    [Fact] void should_preserve_the_cause() => _error.InnerException.ShouldEqual(_cause);
}
