using System.Diagnostics;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Globalization;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var testDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        DirectoryInfo? dir = testDir;
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "VerVal.sln")))
        {
            dir = dir.Parent;
        }

        var webProjectPath = Path.Combine(dir.FullName, "DatesAndStuff.Web", "src", "DatesAndStuff.Web", "DatesAndStuff.Web.csproj");
        if (!File.Exists(webProjectPath))
        {
            throw new FileNotFoundException("Could not find web project file at expected path: " + webProjectPath);
        }

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath) ?? dir.FullName;
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --project \"{webProjectPath}\" --urls \"{BaseURL}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = webProjFolderPath
        };
        
        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }
    
    private static readonly double[] SalaryIncreaseTestCases = { 0d, 5d, 12.5d, -5d, -9.99d };
    [TestCaseSource(nameof(SalaryIncreaseTestCases))]
    public void Person_SalaryIncrease_ShouldIncrease(double salaryIncreasePercentage)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        const double initialSalary = 5000;

        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
        input.Clear();
        input.SendKeys(salaryIncreasePercentage.ToString(CultureInfo.InvariantCulture));

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();


        // Assert
        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var salaryAfterSubmission = double.Parse(salaryLabel.Text);
        var expectedSalary = initialSalary * (100 + salaryIncreasePercentage) / 100;
        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.001);
    }
    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}