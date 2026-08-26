namespace DemoProject.StepDefinitions;

/// <summary>
/// Класс для конфигурации запуска smoke-тестов.
/// </summary>
[Binding]
public static class SmokeRunning
{
    private const int BeforeAllScenarios = -HookAttribute.DefaultOrder * 2;
    private const int BeforeAfterScenarioBlock = -HookAttribute.DefaultOrder * 2;
    private const string VariableName = "TestRunMode";
    private const string ExcludeFromRunningTag = "NoSmoke";

    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
    private static TestRunMode? testRunMode = null;

    /// <summary>
    /// Режим, в котором запускаются автотесты.
    /// </summary>
    public enum TestRunMode
    {
        /// <summary>
        /// Запуск всех выбранных тестов.
        /// </summary>
        Default,

        /// <summary>
        /// Дымовое тестирование. Игнорировать тесты, отмеченные тегом NoSmoke.
        /// </summary>
        Smoke,
    }

    public static TestRunMode Mode
    {
        get
        {
            testRunMode ??= GetTestRunMode();
            return (TestRunMode)testRunMode;
        }
        set { testRunMode = value; }
    }

    [BeforeScenario(Order = BeforeAllScenarios)]
    public static void IgnoreNoSmokeTests(ScenarioContext currentTest)
    {
        if (Mode != TestRunMode.Smoke ||
            currentTest.ScenarioInfo.CombinedTags.Contains(ExcludeFromRunningTag) is false)
        {
            return; // Do nothing.
        }

        logger.Trace("[SmokeRunning] Skipping test...");
        Assert.Ignore($"Test ignored because {VariableName}={Mode}");
    }

    [AfterScenario(Order = BeforeAfterScenarioBlock)]
    public static void IgnoreAfterScenarioBlockOfNoSmokeTests(ScenarioContext currentTest)
    {
        if (Mode != TestRunMode.Smoke ||
            currentTest.ScenarioInfo.CombinedTags.Contains(ExcludeFromRunningTag) is false)
        {
            return; // Do nothing.
        }

        logger.Trace("[SmokeRunning] Skipping AfterScenario...");
        Assert.Ignore($"Test ignored because {VariableName}={Mode}");
    }

    private static TestRunMode GetTestRunMode()
    {
        logger.Debug($"Получение значения {VariableName} из переменной окружения...");
        string testRunModeAsString = Environment.GetEnvironmentVariable(VariableName);

        if (string.IsNullOrEmpty(testRunModeAsString))
        {
            logger.Debug($"Переменная окружения {VariableName} отсутствует.\n" +
                         $"Получение значения {VariableName} из файла конфигурации...");
            testRunModeAsString = ConfigurationManager
                .OpenExeConfiguration("TestAssembly.dll")
                .AppSettings.Settings[VariableName]?
                .Value;

            if (string.IsNullOrEmpty(testRunModeAsString))
            {
                const string errorMessage = $"Не удалось получить значение {VariableName} из переменных среды и файла конфигурации.";
                throw new ArgumentNullException(nameof(testRunModeAsString), errorMessage);
            }
        }

        TestRunMode mode = Enum.Parse<TestRunMode>(testRunModeAsString, ignoreCase: true);
        logger.Trace($"Тесты запускаются в режиме: {mode}");
        return mode;
    }
}