// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.given;

public class IgnoredComplianceMetadataProvider : ICanProvideComplianceMetadataForType, ICanProvideComplianceMetadataForProperty
{
    public bool CanProvide(Type type) => false;
    public bool CanProvide(PropertyInfo property) => false;
    public ComplianceMetadata Provide(Type type) => throw new InvalidOperationException();
    public ComplianceMetadata Provide(PropertyInfo property) => throw new InvalidOperationException();
}
