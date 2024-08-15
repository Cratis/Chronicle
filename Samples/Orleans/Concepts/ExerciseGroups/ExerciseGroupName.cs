namespace Concepts.ExerciseGroups;

public record ExerciseGroupName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ExerciseGroupName NotSet = new(string.Empty);

    public static implicit operator ExerciseGroupName(string value) => new(value);
}