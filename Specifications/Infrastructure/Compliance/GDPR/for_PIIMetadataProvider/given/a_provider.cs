// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.given;

public class a_provider : Specification
{
    protected PIIMetadataProvider provider;

    void Establish() => provider = new();
}
