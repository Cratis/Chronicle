namespace Concepts
{
    public record PersonId(Guid Value) : ConceptAs<Guid>(Value)
    {
        public static implicit operator PersonId(string value) => new(Guid.Parse(value));
    }
}