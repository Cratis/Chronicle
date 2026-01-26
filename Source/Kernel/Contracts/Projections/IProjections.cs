// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractProjectionDefinitionParsingErrors = Cratis.Chronicle.Contracts.Projections.ProjectionDeclarationParsingErrors;
using ContractProjectionPreview = Cratis.Chronicle.Contracts.Projections.ProjectionPreview;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Defines the contract for working with projections.
/// </summary>
[Service]
public interface IProjections
{
    /// <summary>
    /// Register projections.
    /// </summary>
    /// <param name="request">The <see cref="RegisterRequest"/> holding all registrations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterRequest request, CallContext context = default);

    /// <summary>
    /// Get all projection definitions.
    /// </summary>
    /// <param name="request"><see cref="GetAllDefinitionsRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    Task<IEnumerable<ProjectionDefinition>> GetAllDefinitions(GetAllDefinitionsRequest request, CallContext context = default);

    /// <summary>
    /// Get all projection definition.
    /// </summary>
    /// <param name="request"><see cref="GetAllDefinitionsRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ProjectionWithDeclaration"/>.</returns>
    Task<IEnumerable<ProjectionWithDeclaration>> GetAllDeclarations(GetAllDeclarationsRequest request, CallContext context = default);

    /// <summary>
    /// Preview a projection from its declaration representation.
    /// </summary>
    /// <param name="request"><see cref="PreviewProjectionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="OneOf{T0, T1}"/> containing either the <see cref="ContractProjectionPreview"/> or <see cref="ContractProjectionDefinitionParsingErrors"/>.</returns>
    Task<OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>> Preview(PreviewProjectionRequest request, CallContext context = default);

    /// <summary>
    /// Save a projection from its declaration representation.
    /// </summary>
    /// <param name="request"><see cref="SaveProjectionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(SaveProjectionRequest request, CallContext context = default);

    /// <summary>
    /// Generate declarative C# projection code from declaration.
    /// </summary>
    /// <param name="request"><see cref="GenerateDeclarativeCodeRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="OneOf{T0, T1}"/> containing either the <see cref="GeneratedCode"/> or <see cref="ContractProjectionDefinitionParsingErrors"/>.</returns>
    Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateDeclarativeCode(GenerateDeclarativeCodeRequest request, CallContext context = default);

    /// <summary>
    /// Generate model-bound C# read model code from declaration.
    /// </summary>
    /// <param name="request"><see cref="GenerateModelBoundCodeRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="OneOf{T0, T1}"/> containing either the <see cref="GeneratedCode"/> or <see cref="ContractProjectionDefinitionParsingErrors"/>.</returns>
    Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateModelBoundCode(GenerateModelBoundCodeRequest request, CallContext context = default);
}
