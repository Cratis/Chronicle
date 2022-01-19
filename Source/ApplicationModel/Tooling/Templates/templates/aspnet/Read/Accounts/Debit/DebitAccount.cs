using Concepts;
using Concepts.Accounts;

namespace Read.Accounts.Debit
{
    public record DebitAccount(AccountId Id, AccountName Name, PersonId Owner, double Balance);
}