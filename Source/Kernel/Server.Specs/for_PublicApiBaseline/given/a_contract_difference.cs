// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

/// <summary>
/// Supplies an offline rendered contract for planted compatibility breaks.
/// </summary>
public class a_contract_difference : Specification
{
    protected string _baseline = string.Empty;
    protected Exception? _error;

    void Establish() => _baseline = public_api_snapshot.Render([typeof(a_sample_message), a_sample_wire_message.Contract]);

    protected void Compare(string baseline, string current) => _error = Catch.Exception(() => public_api_difference.RequireMatch(baseline, current));
}
