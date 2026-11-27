// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Owns serialized creation and removal for one canonical store/namespace address.</summary>
[Version(1)]
public interface INamespaceIncarnation : IGrainWithStringKey
{
    /// <summary>Ensures and opens the namespace, preserving a prepared identity across recovery.</summary>
    /// <param name="namespace">The requested display name; must belong to this owner.</param>
    /// <returns>The acknowledged canonical name.</returns>
    Task<EventStoreNamespaceName> Ensure(EventStoreNamespaceName @namespace);

    /// <summary>Removes this namespace; pending or terminal recovery is reported as an error.</summary>
    /// <returns>Acknowledged deletion.</returns>
    Task Remove();

    /// <summary>Reconciles persisted pending work on activation, timer, or durable reminder delivery.</summary>
    /// <returns>The bounded recovery turn.</returns>
    Task Wake();

    /// <summary>Resumes the exact observed removal with a fresh bounded budget.</summary>
    /// <param name="operation">The observed removal operation.</param>
    /// <param name="revision">The observed revision.</param>
    /// <returns>The acknowledged recovery request.</returns>
    Task Resume(Guid operation, long revision);

    /// <summary>Aborts the exact observed removal before any destructive or unknown effect.</summary>
    /// <param name="operation">The observed removal operation.</param>
    /// <param name="revision">The observed revision.</param>
    /// <returns>The replacement incarnation's open acknowledgement.</returns>
    Task Abort(Guid operation, long revision);
}
