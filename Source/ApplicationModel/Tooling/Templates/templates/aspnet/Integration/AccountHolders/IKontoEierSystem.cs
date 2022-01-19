namespace Integration.AccountHolders
{
    public interface IKontoEierSystem
    {
        Task<KontoEier> GetBySocialSecurityNumber(string socialSecurityNumber);
        Task<IEnumerable<KontoEier>> GetBySocialSecurityNumbers(IEnumerable<string> socialSecurityNumbers);
    }
}