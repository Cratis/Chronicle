namespace Concepts.Accounts
{
    public record AccountName(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator AccountName(string value) => new(value);
    }
}