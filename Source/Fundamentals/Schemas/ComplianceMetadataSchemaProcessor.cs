// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;
using NJsonSchema.Generation;

namespace Cratis.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemaProcessor"/> for handling compliance metadata.
    /// </summary>
    public class ComplianceMetadataSchemaProcessor : ISchemaProcessor
    {
        readonly IComplianceMetadataResolver _metadataResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceMetadataSchemaProcessor"/> class.
        /// </summary>
        /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
        public ComplianceMetadataSchemaProcessor(IComplianceMetadataResolver metadataResolver)
        {
            _metadataResolver = metadataResolver;
        }

        /// <inheritdoc/>
        public void Process(SchemaProcessorContext context)
        {

        }
    }
}
