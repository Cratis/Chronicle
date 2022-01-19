using Concepts;
using Concepts.Accounts;

namespace Events.Accounts.Debit
{
    [EventType("3daa0bf9-4cca-455e-87bc-c27dade3eb11")]
    public record DebitAccountOpened(AccountName Name, PersonId Owner);
}