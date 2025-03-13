using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract class ScraperBase
{
    protected HttpClient httpClient;
    public static int RequestCount = 0;

    public ScraperBase()
    {
        httpClient = new HttpClient();
    }

    public async Task<HtmlDocument?> GetHtmlDocument(string url)
    {
        try
        {
            using (var driver = InitializeChromeDriver())
            {
                HtmlDocument htmlDoc = await GetDocument(driver, url);

                return htmlDoc;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve HTML document. {ex.Message}");
            return null;
        }
    }

    static IWebDriver InitializeChromeDriver()
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArguments("headless");
        options.AddArgument("--log-level=3");
        options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);  // Deshabilita las notificaciones
        options.AddArgument("--disable-notifications");  // Otra forma de deshabilitar las notificaciones


        // Configura el servicio de ChromeDriver para no emitir salida de consola
        ChromeDriverService service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;  // Suprime la información diagnóstica inicial
        service.HideCommandPromptWindow = true;  // Oculta la ventana de comandos en Windows

        return new ChromeDriver(service, options);
    }

    static async Task<HtmlDocument> GetDocument(IWebDriver driver, string url)
    {
        return await Task.Run(() =>
        {
            try
            {
                RequestCount++;

                Random random = new Random();
                int waitRandom = random.Next(8, 11);

                driver.Navigate().GoToUrl(url);
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitRandom));
                wait.Until(wd => ((IJavaScriptExecutor)wd).ExecuteScript("return document.readyState").ToString() == "complete");

                var htmlContent = driver.PageSource;
                HtmlDocument htmlDoc = new HtmlDocument();

                htmlDoc.LoadHtml(htmlContent);

                return htmlDoc;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HtmlDocument();
            }
        });
    }
}
