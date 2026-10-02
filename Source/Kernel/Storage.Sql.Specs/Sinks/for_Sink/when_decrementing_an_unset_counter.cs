// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_decrementing_an_unset_counter : given.an_initialized_row_with_unset_properties
{
    async Task Because() => await Apply(PropertyMappers.Decrement(_typeFormats, "count", _schema.Properties["count"])(_event, _initial, ArrayIndexers.NoIndexers));

    [Fact] void should_subtract_from_zero() => _stored["count"].ShouldEqual(-1);
}
