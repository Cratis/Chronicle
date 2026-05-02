// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_type;

public class and_type_is_generic_event_source_id_adorned_with_pii : given.a_provider
{
    [PII]
    record MyEntityId(string TypedValue) : EventSourceId<string>(TypedValue);

    Exception _error;

    void Because() => _error = Catch.Exception(() => provider.CanProvide(typeof(MyEntityId)));

    [Fact] void should_throw() => _error.ShouldBeOfExactType<PIINotSupportedOnEventSourceId>();
}
