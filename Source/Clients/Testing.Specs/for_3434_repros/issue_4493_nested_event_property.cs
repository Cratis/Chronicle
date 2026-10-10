// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4493;

/// <summary>
/// A brief.
/// </summary>
/// <param name="Topic">The topic.</param>
public record Brief4493(string Topic);

/// <summary>
/// A content piece was briefed.
/// </summary>
/// <param name="Brief">The brief.</param>
/// <param name="Author">The author.</param>
[EventType]
public record ContentPiece4493Briefed(Brief4493 Brief, string Author);

/// <summary>
/// A draft was generated.
/// </summary>
/// <param name="Revision">The revision.</param>
[EventType]
public record Draft4493Generated(int Revision);

/// <summary>
/// A draft was edited by the author.
/// </summary>
/// <param name="Revision">The revision.</param>
[EventType]
public record Draft4493EditedByAuthor(int Revision);

/// <summary>
/// Compact choice model.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Topic">The topic.</param>
/// <param name="Author">The author.</param>
/// <param name="Revision">The revision.</param>
public record ReviewPiece4493(Guid Id, string Topic, string Author, int Revision);

/// <summary>
/// Projection mapping a nested event property.
/// </summary>
public class ReviewPiece4493Projection : IProjectionFor<ReviewPiece4493>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ReviewPiece4493> builder) => builder
        .NoAutoMap()
        .From<ContentPiece4493Briefed>(from => from
            .Set(piece => piece.Id).ToEventSourceId()
            .Set(piece => piece.Topic).To(briefed => briefed.Brief.Topic)
            .Set(piece => piece.Author).To(briefed => briefed.Author))
        .From<Draft4493Generated>(from => from.Set(piece => piece.Revision).To(e => e.Revision))
        .From<Draft4493EditedByAuthor>(from => from.Set(piece => piece.Revision).To(e => e.Revision));
}

/// <summary>
/// Metadata nested in the read model.
/// </summary>
/// <param name="Topic">The topic.</param>
public record PieceMeta4493(string Topic);

/// <summary>
/// Model with a nested read-model target.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Meta">The metadata.</param>
/// <param name="Revision">The revision.</param>
public record NestedPiece4493(Guid Id, PieceMeta4493 Meta, int Revision);

/// <summary>
/// Projection mapping a nested event property onto a nested read-model property.
/// </summary>
public class NestedPiece4493Projection : IProjectionFor<NestedPiece4493>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<NestedPiece4493> builder) => builder
        .NoAutoMap()
        .From<ContentPiece4493Briefed>(from => from.Set(piece => piece.Meta.Topic).To(briefed => briefed.Brief.Topic))
        .From<Draft4493Generated>(from => from.Set(piece => piece.Revision).To(e => e.Revision))
        .From<Draft4493EditedByAuthor>(from => from.Set(piece => piece.Revision).To(e => e.Revision));
}

public class when_naming_a_nested_property_path : Specification
{
    string _result;

    void Because() => _result = new Cratis.Serialization.CamelCaseNamingPolicy().GetPropertyName("Brief.Topic");

    [Fact] void should_camel_case_every_segment() => _result.ShouldEqual("brief.topic");
}

public class when_only_briefed : Specification
{
    ReadModelScenario<ReviewPiece4493> _scenario;

    void Establish() => _scenario = new ReadModelScenario<ReviewPiece4493>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ContentPiece4493Briefed(new("Event sourcing"), "Ada"));

    [Fact] void should_map_the_nested_topic() => _scenario.Instance!.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_map_the_author() => _scenario.Instance!.Author.ShouldEqual("Ada");
}

public class when_briefed_then_generated_then_edited : Specification
{
    ReadModelScenario<ReviewPiece4493> _scenario;

    void Establish() => _scenario = new ReadModelScenario<ReviewPiece4493>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new ContentPiece4493Briefed(new("Event sourcing"), "Ada"),
        new Draft4493Generated(1),
        new Draft4493EditedByAuthor(2));

    [Fact] void should_retain_the_nested_topic() => _scenario.Instance!.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_retain_the_author() => _scenario.Instance!.Author.ShouldEqual("Ada");
    [Fact] void should_have_latest_revision() => _scenario.Instance!.Revision.ShouldEqual(2);
}

public class when_mapping_onto_a_nested_read_model_property : Specification
{
    ReadModelScenario<NestedPiece4493> _scenario;

    void Establish() => _scenario = new ReadModelScenario<NestedPiece4493>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new ContentPiece4493Briefed(new("Event sourcing"), "Ada"),
        new Draft4493Generated(1),
        new Draft4493EditedByAuthor(2));

    [Fact] void should_retain_the_nested_topic() => _scenario.Instance!.Meta!.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_have_the_meta_object() => _scenario.Instance!.Meta.ShouldNotBeNull();
    [Fact] void should_have_latest_revision() => _scenario.Instance!.Revision.ShouldEqual(2);
}
