// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle;

public partial class EventStore : INamespaceBindings
{
    /// <inheritdoc/>
    public Task<NamespaceBinding> LookupNamespaceBinding(EventStoreNamespaceName @namespace, CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Read(nameof(LookupNamespaceBinding));
        return new NamespaceBindings(_eventStoreName, _servicesAccessor).LookupNamespaceBinding(@namespace, cancellationToken);
    }
}
