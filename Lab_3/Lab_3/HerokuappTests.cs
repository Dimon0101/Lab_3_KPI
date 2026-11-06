using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Lab_3
{
    /// <summary>
    /// Лабораторна робота №3, варіант 2.
    /// Автоматизовані тести сторінок сайту https://the-internet.herokuapp.com/
    /// </summary>
    [TestFixture]
    public class HerokuappTests : BaseTest
    {
        /// <summary>
        /// 1. Forgot Password — наявність форми, введення e-mail (зміна значення поля)
        /// та перехід на сторінку з повідомленням про відправлений лист.
        /// </summary>
        [Test, Order(1)]
        public void Test01_ForgotPassword_SubmitEmail_ShowsConfirmation()
        {
            Open("/forgot_password");

            var heading = Driver.FindElement(By.TagName("h2"));
            var label = Driver.FindElement(By.CssSelector("label[for='email']"));
            var email = Driver.FindElement(By.Id("email"));
            var submit = Driver.FindElement(By.Id("form_submit"));

            Assert.Multiple(() =>
            {
                Assert.That(heading.Text, Is.EqualTo("Forgot Password"));
                Assert.That(label.Text, Is.EqualTo("E-mail"));
                Assert.That(email.Displayed, Is.True, "Поле e-mail не відображається");
                Assert.That(email.Enabled, Is.True, "Поле e-mail недоступне");
                Assert.That(email.GetDomProperty("value"), Is.Empty, "Поле e-mail має бути порожнім");
                Assert.That(submit.Text, Is.EqualTo("Retrieve password"));
            });

            email.SendKeys("student@example.com");
            Assert.That(email.GetDomProperty("value"), Is.EqualTo("student@example.com"));

            submit.Click();
            Wait.Until(d => d.Url.EndsWith("/email_sent"));

            var content = Driver.FindElement(By.Id("content"));
            TakeScreenshot();
            Assert.That(content.Text, Is.EqualTo("Your e-mail's been sent!"));
        }

        /// <summary>
        /// 2. Horizontal Slider — керування повзунком з клавіатури та перевірка,
        /// що текст біля повзунка змінюється відповідно до його значення.
        /// </summary>
        [Test, Order(2)]
        public void Test02_HorizontalSlider_ArrowKeys_ChangeDisplayedValue()
        {
            Open("/horizontal_slider");

            var slider = Driver.FindElement(By.CssSelector("input[type='range']"));
            var range = Driver.FindElement(By.Id("range"));

            Assert.Multiple(() =>
            {
                Assert.That(slider.GetDomAttribute("min"), Is.EqualTo("0.0"));
                Assert.That(slider.GetDomAttribute("max"), Is.EqualTo("5.0"));
                Assert.That(slider.GetDomAttribute("step"), Is.EqualTo("0.5"));
                Assert.That(range.Text, Is.EqualTo("0"), "Початкове значення має бути 0");
            });

            // Три кроки вправо: 0 -> 0.5 -> 1 -> 1.5
            slider.SendKeys(Keys.ArrowRight + Keys.ArrowRight + Keys.ArrowRight);
            Wait.Until(_ => range.Text == "1.5");
            Assert.That(slider.GetDomProperty("value"), Is.EqualTo("1.5"));

            // Крок вліво: 1.5 -> 1
            slider.SendKeys(Keys.ArrowLeft);
            Wait.Until(_ => range.Text == "1");

            // Клавіша End — максимальне значення
            slider.SendKeys(Keys.End);
            Wait.Until(_ => range.Text == "5");

            // Повзунок не виходить за межу max
            slider.SendKeys(Keys.ArrowRight);
            TakeScreenshot();
            Assert.That(range.Text, Is.EqualTo("5"), "Значення не повинно перевищувати max = 5");
        }

        /// <summary>
        /// 3. Dropdown — перелік опцій, стан опції-заглушки, вибір за текстом і за значенням.
        /// </summary>
        [Test, Order(3)]
        public void Test03_Dropdown_SelectOptions_ChangesSelectedValue()
        {
            Open("/dropdown");

            var select = new SelectElement(Driver.FindElement(By.Id("dropdown")));
            var optionTexts = select.Options.Select(o => o.Text).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(select.IsMultiple, Is.False);
                Assert.That(optionTexts, Is.EqualTo(new[] { "Please select an option", "Option 1", "Option 2" }));
                Assert.That(select.SelectedOption.Text, Is.EqualTo("Please select an option"));
                Assert.That(select.Options[0].Enabled, Is.False, "Опція-заглушка має бути неактивною");
            });

            select.SelectByText("Option 1");
            Assert.Multiple(() =>
            {
                Assert.That(select.SelectedOption.Text, Is.EqualTo("Option 1"));
                Assert.That(select.SelectedOption.GetDomProperty("value"), Is.EqualTo("1"));
            });

            select.SelectByValue("2");
            TakeScreenshot();
            Assert.Multiple(() =>
            {
                Assert.That(select.SelectedOption.Text, Is.EqualTo("Option 2"));
                Assert.That(select.Options[1].Selected, Is.False, "Option 1 має втратити вибір");
            });
        }

        /// <summary>
        /// 4. Typos — сторінка випадково вносить одруківку в текст. Тест перезавантажує
        /// сторінку кілька разів, перевіряє, що текст приймає лише два відомі варіанти,
        /// і виявляє одруківку «won,t» замість «won't».
        /// </summary>
        [Test, Order(4)]
        public void Test04_Typos_ReloadPage_DetectsTypoInText()
        {
            const string correct = "Sometimes you'll see a typo, other times you won't.";
            const string withTypo = "Sometimes you'll see a typo, other times you won,t.";
            const int reloads = 15;

            Open("/typos");
            var seenTexts = new HashSet<string>();
            int typoCount = 0;

            for (int i = 0; i < reloads; i++)
            {
                if (i > 0) Driver.Navigate().Refresh();

                var paragraphs = Driver.FindElements(By.CssSelector("div.example p"));
                Assert.That(paragraphs, Has.Count.EqualTo(2), "На сторінці мають бути 2 абзаци");
                Assert.That(paragraphs[0].Text,
                    Is.EqualTo("This example demonstrates a typo being introduced. It does it randomly on each page load."));

                var text = paragraphs[1].Text;
                seenTexts.Add(text);
                if (text == withTypo && typoCount++ == 0)
                    TakeScreenshot(); // фіксуємо сторінку з одруківкою
            }

            TestContext.Out.WriteLine($"Одруківку виявлено в {typoCount} з {reloads} завантажень.");
            Assert.Multiple(() =>
            {
                Assert.That(seenTexts, Is.SubsetOf(new[] { correct, withTypo }), "З'явився невідомий варіант тексту");
                Assert.That(typoCount, Is.GreaterThan(0), "За всі перезавантаження одруківку не виявлено");
            });
        }

        /// <summary>
        /// 5. Entry Ad — модальне вікно з'являється після завантаження сторінки,
        /// містить очікуваний текст і зникає після натискання «Close».
        /// </summary>
        [Test, Order(5)]
        public void Test05_EntryAd_ModalWindow_AppearsAndCloses()
        {
            Open("/entry_ad");

            var modal = Driver.FindElement(By.Id("modal"));
            Wait.Until(_ => modal.Displayed);

            var title = modal.FindElement(By.CssSelector(".modal-title h3"));
            var body = modal.FindElement(By.CssSelector(".modal-body p"));
            var close = modal.FindElement(By.CssSelector(".modal-footer p"));

            TakeScreenshot();
            Assert.Multiple(() =>
            {
                Assert.That(title.Text, Is.EqualTo("This is a modal window").IgnoreCase);
                Assert.That(body.Text, Does.StartWith("It's commonly used to encourage a user to take an action"));
                Assert.That(close.Text, Is.EqualTo("Close").IgnoreCase);
                Assert.That(Driver.FindElement(By.Id("page-footer")).Displayed, Is.False,
                    "Поки відкрито вікно, футер сторінки прихований");
            });

            close.Click();
            Wait.Until(_ => !modal.Displayed);

            Assert.Multiple(() =>
            {
                Assert.That(modal.Displayed, Is.False, "Модальне вікно мало закритися");
                Assert.That(Driver.FindElement(By.Id("page-footer")).Displayed, Is.True);
                Assert.That(Driver.FindElement(By.Id("restart-ad")).Text, Is.EqualTo("click here"));
            });
        }

        /// <summary>
        /// 6. File Download — завантаження файлу за посиланням і перевірка, що файл
        /// з'явився на диску та збігається з файлом на сервері.
        /// </summary>
        [Test, Order(6)]
        public async Task Test06_FileDownload_ClickLink_FileSavedToDisk()
        {
            Open("/download");

            var links = Driver.FindElements(By.CssSelector("div.example a"));
            Assert.That(links, Is.Not.Empty, "На сторінці немає файлів для завантаження");

            // Беремо перший .txt-файл (список файлів на сайті може змінюватися)
            var link = links.FirstOrDefault(l => l.Text.EndsWith(".txt")) ?? links[0];
            var fileName = link.Text;
            var fileUrl = $"{BaseUrl}/download/{fileName}";
            Assert.That(link.GetDomAttribute("href"), Is.EqualTo("download/" + fileName));

            link.Click();

            var path = Path.Combine(DownloadDir, fileName);
            var downloaded = TryWait(_ => File.Exists(path), 30);
            Assert.That(downloaded, Is.True, $"Файл {fileName} не завантажився за 30 с");

            using var http = new HttpClient();
            var expected = await http.GetByteArrayAsync(fileUrl);
            var actual = await File.ReadAllBytesAsync(path);

            // Скріншот сторінки завантажень Chrome як підтвердження
            Driver.Navigate().GoToUrl("chrome://downloads/");
            TakeScreenshot();

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.Not.Empty, "Завантажений файл порожній");
                Assert.That(actual, Is.EqualTo(expected), "Вміст файлу відрізняється від файлу на сервері");
            });
        }

        /// <summary>
        /// 7. Basic Auth — вхід на сторінку з HTTP Basic-авторизацією
        /// (облікові дані admin/admin передаються в URL).
        /// </summary>
        [Test, Order(7)]
        public void Test07_BasicAuth_ValidCredentials_ShowsCongratulations()
        {
            Driver.Navigate().GoToUrl("https://admin:admin@the-internet.herokuapp.com/basic_auth");

            var heading = Wait.Until(d => d.FindElement(By.CssSelector("div.example h3")));
            var message = Driver.FindElement(By.CssSelector("div.example p"));

            TakeScreenshot();
            Assert.Multiple(() =>
            {
                Assert.That(heading.Text, Is.EqualTo("Basic Auth"));
                Assert.That(message.Text, Is.EqualTo("Congratulations! You must have the proper credentials."));
                Assert.That(Driver.Url, Does.EndWith("/basic_auth"));
            });
        }

        /// <summary>
        /// 8. Dynamic Loading — елемент існує в DOM, але прихований; після натискання
        /// «Start» з'являється індикатор завантаження, а потім текст «Hello World!».
        /// </summary>
        [Test, Order(8)]
        public void Test08_DynamicLoading_ClickStart_HiddenElementBecomesVisible()
        {
            Open("/dynamic_loading");
            Driver.FindElement(By.LinkText("Example 1: Element on page that is hidden")).Click();
            Wait.Until(d => d.Url.EndsWith("/dynamic_loading/1"));

            var finish = Driver.FindElement(By.Id("finish"));
            var start = Driver.FindElement(By.CssSelector("#start button"));

            Assert.Multiple(() =>
            {
                Assert.That(finish.Displayed, Is.False, "До натискання Start результат прихований");
                Assert.That(start.Text, Is.EqualTo("Start"));
            });

            start.Click();

            var loading = Wait.Until(d => d.FindElement(By.Id("loading")));
            Assert.That(loading.Text, Does.Contain("Loading..."));

            // Явне очікування замість Thread.Sleep: чекаємо до 15 с, поки з'явиться результат
            Wait.Until(_ => finish.Displayed);

            TakeScreenshot();
            Assert.Multiple(() =>
            {
                Assert.That(finish.Text, Is.EqualTo("Hello World!"));
                Assert.That(loading.Displayed, Is.False, "Індикатор завантаження має зникнути");
                Assert.That(Driver.FindElement(By.Id("start")).Displayed, Is.False, "Кнопка Start має зникнути");
            });
        }

        /// <summary>
        /// 9. Context Menu — правий клік по області викликає JavaScript-алерт;
        /// перевіряємо текст алерту та його закриття.
        /// </summary>
        [Test, Order(9)]
        public void Test09_ContextMenu_RightClick_ShowsAlert()
        {
            Open("/context_menu");

            var hotSpot = Driver.FindElement(By.Id("hot-spot"));
            Assert.That(hotSpot.Displayed, Is.True);

            new Actions(Driver).ContextClick(hotSpot).Perform();

            var alert = Wait.Until(d =>
            {
                try { return d.SwitchTo().Alert(); }
                catch (NoAlertPresentException) { return null; }
            });

            Assert.That(alert!.Text, Is.EqualTo("You selected a context menu"));
            alert.Accept();

            Assert.Throws<NoAlertPresentException>(() => Driver.SwitchTo().Alert(),
                "Після Accept алерт має закритися");

            TakeScreenshot();
            Assert.That(Driver.FindElement(By.TagName("h3")).Text, Is.EqualTo("Context Menu"));
        }

        /// <summary>
        /// 10. Redirect Link — у новій вкладці переходимо за посиланням-редиректом,
        /// перевіряємо сторінку Status Codes, потім закриваємо вкладку та
        /// повертаємося до початкового вікна.
        /// </summary>
        [Test, Order(10)]
        public void Test10_RedirectLink_NewTab_RedirectsToStatusCodes()
        {
            Open("/");
            var originalWindow = Driver.CurrentWindowHandle;

            Driver.SwitchTo().NewWindow(WindowType.Tab);
            Assert.That(Driver.WindowHandles, Has.Count.EqualTo(2));

            Open("/redirector");
            var redirectLink = Driver.FindElement(By.Id("redirect"));
            Assert.That(redirectLink.Text, Is.EqualTo("here"));

            redirectLink.Click();
            Wait.Until(d => d.Url.EndsWith("/status_codes"));

            var codes = Driver.FindElements(By.CssSelector("div.example ul li a")).Select(a => a.Text).ToList();
            TakeScreenshot();
            Assert.Multiple(() =>
            {
                Assert.That(Driver.FindElement(By.TagName("h3")).Text, Is.EqualTo("Status Codes"));
                Assert.That(codes, Is.EqualTo(new[] { "200", "301", "404", "500" }));
            });

            Driver.FindElement(By.LinkText("200")).Click();
            Wait.Until(d => d.Url.EndsWith("/status_codes/200"));
            Assert.That(Driver.FindElement(By.CssSelector("div.example p")).Text,
                Does.StartWith("This page returned a 200 status code."));

            Driver.Close();
            Driver.SwitchTo().Window(originalWindow);

            Assert.Multiple(() =>
            {
                Assert.That(Driver.WindowHandles, Has.Count.EqualTo(1));
                Assert.That(Driver.FindElement(By.TagName("h1")).Text, Is.EqualTo("Welcome to the-internet"));
            });
        }
    }
}
