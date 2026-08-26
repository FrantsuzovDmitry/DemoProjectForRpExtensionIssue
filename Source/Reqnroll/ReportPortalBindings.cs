using System.Reflection;
using Microsoft.Extensions.Configuration;
using ReportPortal.Client.Abstractions.Filtering;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Abstractions.Responses;

namespace DemoProject.Reqnroll;

[Binding]
public class ReportPortalBindings
{
    private static string launchDescription;

    /// <summary>
    /// Возвращает значение параметра из переменной окружения.
    /// </summary>
    /// <param name="name">Имя параметра.</param>
    /// <returns>Значение.</returns>
    private static string GetEnvironmentParameter(string name)
    {
        try
        {
            return Environment.GetEnvironmentVariable(name);
        }
        catch
        {
            return string.Empty;
        }
    }

    [BeforeTestRun(Order = -30000)]
    private static void BeforeTestRun()
    {
        IConfiguration launchInfo = GetLaunchInfo();
        if (launchInfo["enabled"].Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            // Если логгирование отключено, никак не трогаем ReportPortal,
            // потому что иначе, при отсутствии сети, тесты не запустятся.
            return;
        }

        // прикапываем информацию о запуске, т.к. при rerun'е она затирается.
        LaunchResponse launch = GetLaunch(launchInfo);
        launchDescription = launch?.Description;

        // Регистрация событий ReportPortal.
        ReportPortalAddin.BeforeRunStarted += BeforeRunStarted;
        ReportPortalAddin.BeforeFeatureStarted += BeforeFeatureStarted;
        ReportPortalAddin.BeforeScenarioStarted += BeforeFeatureScenarioStarted;
    }

    private static LaunchResponse GetLaunch(IConfiguration launchInfo)
    {
        ReportPortal.Client.Service service = new(
            uri: new Uri(launchInfo["server:url"]),
            projectName: launchInfo["server:project"],
            token: launchInfo["server:apiKey"]
        );

        string uuid = GetEnvironmentParameter("ReportPortal_Launch_Id");       // задается на CI.
        if (string.IsNullOrEmpty(uuid) is false)
        {
            return service.Launch.GetAsync(uuid)?.Result
                ?? throw new Exception($"Запуск с uuid {uuid} не найден.");
        }

        string launchName = launchInfo["launch:name"];
        FilterOption opts = new()
        {
            Paging = new Paging(number: 1, size: 100)   // Первые 100 запусков. Этого должно быть достаточно.
        };
        Content<LaunchResponse> launhes = service.Launch.GetAsync(opts).Result;
        LaunchResponse result = launhes.Items
            .Where(l => l.Name == launchName)
            .OrderBy(l => l.EndTime)
            .LastOrDefault();

        if (result is null)
        {
            return CreateLaunch(launchInfo, service);
        }

        return result;
    }

    private static LaunchResponse CreateLaunch(IConfiguration launchInfo, ReportPortal.Client.Service service)
    {
        StartLaunchRequest startLaunchRequest = new()
        {
            Name = launchInfo["launch:name"],
            Description = launchInfo["launch:description"], // опционально
            StartTime = DateTime.UtcNow,
        };
        LaunchCreatedResponse createdLaunch = service.Launch.StartAsync(startLaunchRequest).Result;

        return service.Launch.GetAsync(createdLaunch.Uuid).Result;
    }

    private static IConfiguration GetLaunchInfo()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("ReportPortal.config.json")
            .Build();

        return config;
    }

    private static void BeforeRunStarted(object sender, RunStartedEventArgs e)
    {
        e.StartLaunchRequest.IsRerun = true;
        e.StartLaunchRequest.Description = launchDescription;
    }

	private static void BeforeFeatureScenarioStarted(object sender, TestItemStartedEventArgs e)
    {
        // ...
    }

	private static void BeforeFeatureStarted(object sender, TestItemStartedEventArgs e)
    {
        // ...
    }
}