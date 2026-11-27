// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Transactions;

/// <summary>An opt-in namespace identity capability, separate from the existing unit of work interface.</summary>
public interface INamespaceBoundUnitOfWork
{
    /// <summary>Pins a snapshot before decision reads or staging. A different snapshot can never replace it.</summary>
    /// <param name="binding">The open namespace snapshot.</param>
    void BindNamespace(NamespaceBinding binding);
}
