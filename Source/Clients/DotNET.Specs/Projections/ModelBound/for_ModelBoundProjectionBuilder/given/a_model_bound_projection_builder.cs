// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

public class a_model_bound_projection_builder : Specification
{
    protected ModelBoundProjectionBuilder builder;
    protected INamingPolicy naming_policy;
    protected IEventTypes event_types;
    protected JsonSerializerOptions json_serializer_options;

    void Establish()
    {
        naming_policy = Substitute.For<INamingPolicy>();
        naming_policy.GetPropertyName(Arg.Any<Properties.PropertyPath>())
            .Returns(ci => ci.ArgAt<Properties.PropertyPath>(0).Path);

        event_types = new EventTypesForSpecifications([
            typeof(DebitAccountOpened),
            typeof(DepositToDebitAccountPerformed),
            typeof(WithdrawalFromDebitAccountPerformed),
            typeof(ItemAddedToCart)
        ]);

        json_serializer_options = new JsonSerializerOptions();

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types, json_serializer_options);
    }
}
