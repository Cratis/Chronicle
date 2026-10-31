// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_non_default_culture : a_composite_projection
{
    readonly Dictionary<string, object?> _parts = Parts("I-tenant", 1234567890.123456789m);
    string _nativeIdentity;
    CompositeSeedModel _result;

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("I-tenant", 1234567890.123456789m, "changed"));
        _nativeIdentity = _scenario.Instances.Keys.Single().Value;
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            await _scenario.Given.ForKey(_parts).ReadModel(new(new("I-tenant", 1234567890.123456789m), "seed", "retained", 5));
            _result = _scenario.InstanceForKey(_parts)!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact] void should_update_the_seed() => _result.Count.ShouldEqual(6);
    [Fact] void should_preserve_decimal_precision() => _result.Id.Amount.ShouldEqual(1234567890.123456789m);
    [Fact] void should_use_the_same_native_identity() => _scenario.Instances.Keys.Single().Value.ShouldEqual(_nativeIdentity);
}
