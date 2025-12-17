// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

public class a_model_bound_projection_builder : Specification
{
    internal ModelBoundProjectionBuilder builder;
    protected INamingPolicy naming_policy;
    protected IEventTypes event_types;

    void Establish()
    {
        naming_policy = new TestNamingPolicy();
        event_types = new EventTypesForSpecifications([
            typeof(DebitAccountOpened),
            typeof(DepositToDebitAccountPerformed),
            typeof(WithdrawalFromDebitAccountPerformed),
            typeof(ItemAddedToCart),
            typeof(ReadModelRemoved),
            typeof(ReadModelRemovedJoin),
            typeof(ChildItemRemoved),
            typeof(ChildItemRemovedJoin)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }
}
