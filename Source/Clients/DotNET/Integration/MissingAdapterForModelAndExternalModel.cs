// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Integration;

/// <summary>
/// Exception that gets thrown when an <see cref="IAdapterFor{TModel, TExternalModel}"/> is missing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingAdapterForModelAndExternalModel"/> class.
/// </remarks>
/// <param name="model">Type of model.</param>
/// <param name="externalModel">Type of external model.</param>
public class MissingAdapterForModelAndExternalModel(Type model, Type externalModel) : Exception($"Missing adapter for model '{model.FullName}' and external model '{externalModel.FullName}'")
{
}
