// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Exception that gets thrown when an <see cref="IAdapterFor{TModel, TExternalModel}"/> is missing.
    /// </summary>
    public class MissingAdapterForModelAndExternalModel : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingAdapterForModelAndExternalModel"/> class.
        /// </summary>
        /// <param name="model">Type of model.</param>
        /// <param name="externalModel">Type of external model.</param>
        public MissingAdapterForModelAndExternalModel(Type model, Type externalModel)
            : base($"Missing adapter for model '{model.FullName}' and external model '{externalModel.FullName}'")
        {
        }
    }
}
