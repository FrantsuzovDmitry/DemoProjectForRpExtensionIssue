namespace DemoProject.StepDefinitions;

[Binding]
[Scope(Feature = "Tests")]
internal class Tests
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    [BeforeScenario]
    public void BeforeTest()
    {
        logger.Info("[BeforeScenario] BeforeScenario running...");
    }

    [AfterScenario]
    public void AfterTest()
    {
        if (TestContext.CurrentContext.Result.Outcome.Equals(TestStatus.Skipped))
        {
            logger.Info("[AfterScenario] Ignore after scenario...");
            return;
        }

        logger.Info("[AfterScenario] After scenario running ...");
    }

    [Given("DoSmth")]
    public void DoSmth()
    {
        logger.Info("Doing something ...");
    }
}
