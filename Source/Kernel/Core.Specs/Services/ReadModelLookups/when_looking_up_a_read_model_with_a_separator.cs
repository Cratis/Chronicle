// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModelLookups;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModelLookups;

public class when_looking_up_a_read_model_with_a_separator : Specification
{
    [Fact]
    public async Task should_refuse_before_any_definition_lookup_or_materialized_store_access()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var store = Substitute.For<IMaterializedReadModelStore>();
        var service = new ReadModelLookups(grainFactory, store, Substitute.For<ITypeFormats>(), Substitute.For<IExpandoObjectConverter>(), new JsonSerializerOptions(), NullLogger<ReadModelLookups>.Instance);

        var result = await service.FindByCompositeKey(new FindByCompositeKeyRequest
        {
            EventStore = "application",
            Namespace = "Default",
            ReadModel = "x#System"
        });

        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Single().Message.ShouldEqual(global::Cratis.Chronicle.Security.SystemStoreAccess.SeparatorRefused);
        grainFactory.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().ShouldBeEmpty();
    }
}
