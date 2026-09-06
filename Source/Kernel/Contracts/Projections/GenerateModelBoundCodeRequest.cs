// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for generating model-bound C# read model code from projection declaration language.
/// </summary>
[ProtoContract]
public class GenerateModelBoundCodeRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projection declaration language representation of the projection.
    /// </summary>
    [ProtoMember(3)]
    public string Declaration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional draft read model definition to use for code generation.
    /// When provided, this will be used instead of looking up an existing read model.
    /// </summary>
    [ProtoMember(4)]
    public DraftReadModelDefinition? DraftReadModel { get; set; }

    /// <summary>
    /// Gets or sets the language to generate for.
    /// </summary>
    /// <remarks>
    /// Optional. A request that does not set it generates C#, which is what every caller got before
    /// there was a choice - so an older client keeps working unchanged.
    /// </remarks>
    [ProtoMember(5)]
    public ProjectionCodeLanguage Language { get; set; } = ProjectionCodeLanguage.CSharp;
}
