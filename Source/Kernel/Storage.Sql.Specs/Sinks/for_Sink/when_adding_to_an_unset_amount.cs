// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_adding_to_an_unset_amount : given.an_initialized_row_with_unset_properties
{
    async Task Because() => await Apply(PropertyMappers.AddWithEventValueProvider(_typeFormats, "amount", _schema.Properties["amount"], _ => 2.5)(_event, _initial, ArrayIndexers.NoIndexers));

    [Fact] void should_add_to_zero() => _stored["amount"].ShouldEqual(2.5);
}
