using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Lab_3
{
    /// <summary>
    /// Базовий клас для всіх UI-тестів: перед кожним тестом відкриває новий
    /// екземпляр Chrome, після тесту закриває його. Містить допоміжні методи
    /// для навігації, явних очікувань і створення скріншотів.
    /// </summary>
    public abstract class BaseTest
    {
        protected const string BaseUrl = "https://the-internet.herokuapp.com";

        protected IWebDriver Driver = null!;
        protected WebDriverWait Wait = null!;

        /// <summary>Папка, куди Chrome зберігає завантажені файли.</summary>
        protected string DownloadDir = null!;

        /// <summary>Папка зі скріншотами результатів (bin/Debug/net10.0/Screenshots).</summary>
        protected string ScreenshotDir = null!;

        [SetUp]
        public void SetUp()
        {
            var workDir = TestContext.CurrentContext.WorkDirectory;
            ScreenshotDir = Path.Combine(workDir, "Screenshots");
            DownloadDir = Path.Combine(workDir, "Downloads");

            Directory.CreateDirectory(ScreenshotDir);
            if (Directory.Exists(DownloadDir))
                Directory.Delete(DownloadDir, recursive: true);
            Directory.CreateDirectory(DownloadDir);

            var options = new ChromeOptions();
            options.AddArgument("--window-size=1280,900");
            options.AddUserProfilePreference("download.default_directory", DownloadDir);
            options.AddUserProfilePreference("download.prompt_for_download", false);

            // Змінна середовища HEADLESS=1 запускає браузер без вікна (для CI / командного рядка).
            if (Environment.GetEnvironmentVariable("HEADLESS") == "1")
                options.AddArgument("--headless=new");

            // Selenium Manager сам знаходить або завантажує chromedriver потрібної версії.
            Driver = new ChromeDriver(options);
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                    TakeScreenshot("FAILED");
            }
            catch (WebDriverException)
            {
                // Скріншот неможливий (наприклад, відкритий алерт) — не маскуємо основну помилку.
            }
            finally
            {
                Driver.Quit();
                Driver.Dispose();
            }
        }

        /// <summary>Відкриває сторінку сайту за відносним шляхом.</summary>
        protected void Open(string path) => Driver.Navigate().GoToUrl(BaseUrl + path);

        /// <summary>
        /// Зберігає скріншот поточного стану сторінки у файл &lt;ІмʼяТесту&gt;[_suffix].png
        /// і прикріплює його до результату тесту (видно у Test Explorer).
        /// </summary>
        protected string TakeScreenshot(string suffix = "")
        {
            var name = TestContext.CurrentContext.Test.MethodName
                       + (string.IsNullOrEmpty(suffix) ? "" : "_" + suffix) + ".png";
            var file = Path.Combine(ScreenshotDir, name);
            ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(file);
            TestContext.AddTestAttachment(file);
            return file;
        }

        /// <summary>
        /// Чекає виконання умови не довше за <paramref name="seconds"/> секунд.
        /// Повертає false замість винятку, якщо умова так і не виконалась.
        /// </summary>
        protected bool TryWait(Func<IWebDriver, bool> condition, double seconds)
        {
            try
            {
                return new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(condition);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
    }
}
