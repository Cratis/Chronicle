using Dolittle.SDK.Events;

namespace Dolittle.Customers
{
    [EventType("5e07940e-7fd0-4b26-beaf-606e8917dd57")]
    public record CustomerSignedUp(SocialSecurityNumber SocialSecurityNumber, FirstName FirstName, LastName LastName);
}
