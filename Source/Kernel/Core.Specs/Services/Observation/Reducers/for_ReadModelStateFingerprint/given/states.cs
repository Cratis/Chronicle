// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint.given;

public class states : Specification
{
    protected static ulong Of(object? value)
    {
        ReadModelStateFingerprint.TryCompute(value, out var fingerprint).ShouldBeTrue();
        return fingerprint;
    }

    protected static ExpandoObject Expando(params (string Key, object? Value)[] entries)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;
        foreach (var (key, value) in entries)
        {
            dictionary[key] = value;
        }

        return expando;
    }
}
