// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Exception that gets thrown when the AST Document does not contain any projections.
/// </summary>
public class DocumentMustHaveAtLeastOneProjection() : Exception("Document must contain at least one projection");
