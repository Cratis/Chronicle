// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Integration;
using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

#pragma warning disable CA1051, SA1600, RCS1213, IDE0051, CS8618

/// <summary>
/// Represents a specialized <see cref="Specification"/> for specifying behaviors of adapters.
/// </summary>
/// <typeparam name="TModel">The model the adapter is for.</typeparam>
/// <typeparam name="TExternalModel">The external model the adapter is for.</typeparam>
public abstract class AdapterSpecificationFor<TModel, TExternalModel> : Specification
{
    protected IAdapterFor<TModel, TExternalModel> adapter;
    protected AdapterSpecificationContext<TModel, TExternalModel> context;

    /// <summary>
    /// Create an instance of the adapter for the specification.
    /// </summary>
    /// <returns>A new instance of the adapter.</returns>
    protected abstract IAdapterFor<TModel, TExternalModel> CreateAdapter();

    void Establish()
    {
        adapter = CreateAdapter();
        context = new(adapter);
    }
}
