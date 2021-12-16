using Cratis.Compliance;

namespace Dolittle.Customers
{
    public record LastName(string Value) : PIIConceptAs<string>(Value);
}
