// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_signing_with_a_non_invariant_culture : given.a_signing_fixture
{
    CultureInfo _originalCulture;
    string _signature;

    void Establish()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        CultureInfo.CurrentCulture = culture;
    }

    void Because() => _signature = WebhookSigning.Sign("msg_fixture", -1, [], _secret);

    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;

    /// <summary>
    /// Independent Node.js node:crypto HMAC-SHA256 fixture, secret bytes 0..31.
    /// Frame hex: 6d73675f666978747572652e2d312e (msg_fixture.-1. with an empty payload).
    /// This pins invariant formatting, not a delivery policy admitting pre-epoch attempts.
    /// </summary>
    [Fact] void should_format_unix_seconds_invariantly() => _signature.ShouldEqual("v1,YhX26wu5/fI5+Zo+lP0cZ0DWn+IdULWB67US4SxyhJ8=");
}
