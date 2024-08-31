namespace Concepts.ExerciseGroups;

public record ExerciseGroupId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ExerciseGroupId NotSet = new(Guid.Empty);

    public static implicit operator ExerciseGroupId(Guid value) => new(value);

    public static ExerciseGroupId New() => new(Guid.NewGuid());
}
