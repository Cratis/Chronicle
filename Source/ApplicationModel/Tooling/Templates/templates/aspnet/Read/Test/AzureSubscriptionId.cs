// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Azure
{
    public record AzureSubscriptionId(Guid Value) : ConceptAs<Guid>(Value)
    {
        public static implicit operator AzureSubscriptionId(string value) => new(Guid.Parse(value));
    }
}
