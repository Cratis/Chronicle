using Concepts;
using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit
{
    public record CreateDebitAccount(AccountId AccountId, AccountName Name, PersonId Owner);
}