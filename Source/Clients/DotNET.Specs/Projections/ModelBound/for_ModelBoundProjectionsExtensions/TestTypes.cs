// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions;

public record SomeEvent(string Name);
public record AnotherEvent(int Amount);

[FromEvent<SomeEvent>]
public record TypeWithFromEventAttribute([Key] Guid Id, string Name);

[FromEventSequence("custom-sequence")]
public record TypeWithFromEventSequenceAttribute([Key] Guid Id, string Name);

[Passive]
public record TypeWithPassiveAttribute([Key] Guid Id, string Name);

[NotRewindable]
public record TypeWithNotRewindableAttribute([Key] Guid Id, string Name);

public record TypeWithConstructorParameterAnnotation(
    [Key] Guid Id,
    [SetFrom<SomeEvent>(nameof(SomeEvent.Name))] string Name);

public record TypeWithMultipleConstructorParameterAnnotations(
    [Key] Guid Id,
    [SetFrom<SomeEvent>(nameof(SomeEvent.Name))] string Name,
    [AddFrom<AnotherEvent>(nameof(AnotherEvent.Amount))] int Amount);

public class TypeWithPropertyAnnotation
{
    [Key]
    public Guid Id { get; set; }

    [SetFrom<SomeEvent>(nameof(SomeEvent.Name))]
    public string Name { get; set; } = string.Empty;
}

public class TypeWithMultiplePropertyAnnotations
{
    [Key]
    public Guid Id { get; set; }

    [SetFrom<SomeEvent>(nameof(SomeEvent.Name))]
    public string Name { get; set; } = string.Empty;

    [AddFrom<AnotherEvent>(nameof(AnotherEvent.Amount))]
    public int Amount { get; set; }

    [ChildrenFrom<SomeEvent>]
    public IEnumerable<string> Items { get; set; } = [];
}

public class TypeWithFromEveryAttribute
{
    [Key]
    public Guid Id { get; set; }

    [FromEvery(contextProperty: "Occurred")]
    public DateTimeOffset LastUpdated { get; set; }
}

public record PlainTypeWithoutAnnotations([Key] Guid Id, string Name);

public class PlainClassWithoutAnnotations
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TypeWithMultipleConstructors
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }

    public TypeWithMultipleConstructors()
    {
    }

    public TypeWithMultipleConstructors(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeWithMultipleConstructors"/> class.
    /// This should be selected as primary (most parameters).
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="amount">The amount.</param>
    public TypeWithMultipleConstructors(
        Guid id,
        [SetFrom<SomeEvent>(nameof(SomeEvent.Name))] string name,
        int amount)
    {
        Id = id;
        Name = name;
        Amount = amount;
    }
}

public record TypeWithConstructorParametersButNoAnnotations(
    [Key] Guid Id,
    string Name,
    int Amount);

public class TypeWithNoPublicConstructors
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    private TypeWithNoPublicConstructors()
    {
    }
}

public abstract class AbstractTypeWithAnnotations
{
    [Key]
    public Guid Id { get; set; }

    [SetFrom<SomeEvent>(nameof(SomeEvent.Name))]
    public string Name { get; set; } = string.Empty;
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
