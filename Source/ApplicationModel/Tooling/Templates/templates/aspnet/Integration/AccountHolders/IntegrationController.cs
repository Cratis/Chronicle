namespace Integration.AccountHolders
{
    [Route("/api/integration")]

    public class IntegrationController : Controller
    {
        readonly KontoEierConnector _connector;

        public IntegrationController(KontoEierConnector connector)
        {
            _connector = connector;
        }

        [HttpGet]
        public async Task Trigger()
        {
            await _connector.ImportOne("03050712345");
        }
    }
}
