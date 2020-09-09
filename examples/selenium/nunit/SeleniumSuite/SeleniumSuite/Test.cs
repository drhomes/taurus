using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;


namespace SeleniumSuite
{
    [TestFixture()]
    public class Test
    {
		IWebDriver driver = null;

		[SetUp]
		public void Initialize()
		{
			driver = new ChromeDriver();
			TestContext.WriteLine($"Test ID: {TestContext.CurrentContext.Test.ID}");
			if (TestContext.Parameters.Exists("UserName"))
            {
				TestContext.WriteLine($"Current Test User: {TestContext.Parameters["UserName"]}");
			}
			if (TestContext.Parameters.Exists("Password"))
			{
				TestContext.WriteLine($"Current Test Password: {TestContext.Parameters["Password"].ToString()}");
			}
			if (TestContext.Parameters.Exists("IterationId"))
			{
				TestContext.WriteLine($"Current Test Iteration: {TestContext.Parameters["IterationId"].ToString()}");
			}
		}

		[Test()]
		public void IndexPage()
		{
			driver.Navigate().GoToUrl("http://blazedemo.com");
			Assert.AreEqual(driver.Title, "BlazeDemo");
		}

		[Test()]
		public void ReservePage()
		{
			driver.Navigate().GoToUrl("http://blazedemo.com/reserve.php");
			Assert.AreEqual(driver.Title, "BlazeDemo - reserve");
		}

	    [TearDown]
		public void AfterTest()
		{
			if (this.driver != null)
				driver.Close();
		}
	}
}
