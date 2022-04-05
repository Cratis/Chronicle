// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration;

/// <summary>
/// Defines a builder for building how to handle input changes.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TExternalModel">The type of the external model.</typeparam>
public interface IImportBuilderFor<TModel, TExternalModel> : IObservable<ImportContext<TModel, TExternalModel>>
{
}
