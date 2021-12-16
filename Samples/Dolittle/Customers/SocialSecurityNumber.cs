using Cratis.Compliance;

namespace Dolittle.Customers
{
    public record SocialSecurityNumber(string Value) : PIIConceptAs<string>(Value);
}
