// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier.given;

public class a_classifier : Specification
{
    protected ResourceNameAuditResult _result;

    protected static ResourceOwner Store(string name) => new(name);

    protected static ResourceOwner Namespace(string store, string name) => new(store, name);

    protected static ResourceUse Use(ResourceOwner? owner, string identity, ResourceKind kind = ResourceKind.EventStoreDatabase) => new(owner, kind, identity);

    protected ResourceNameAuditResult Classify(IEnumerable<ResourceUse> uses, ResourceNameExemptions? exemptions = null) =>
        ResourceNameClassifier.Classify(uses, exemptions ?? ResourceNameExemptions.None);
}
