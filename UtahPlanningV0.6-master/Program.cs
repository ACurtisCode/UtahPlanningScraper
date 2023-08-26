using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using HtmlAgilityPack;
using Microsoft.JSInterop;
using MongoDB.Driver;
using MongoDB.Bson;

namespace UtahPlanningV0._6
{
    public static class Program
    {

        public static void Main(string[] args)
        {


            // generates frontend web page
            CreateHostBuilder(args).Build().Run();
        }
        
        private static void WriteList(Dictionary<string, string> pdfList)
        {
            var settings = new MongoClientSettings();
            var dbClient = new MongoClient();


            // Connects to external MongoDB Atlas
            // Attempts connections and prints exception
            try
            {
                settings = MongoClientSettings.FromConnectionString("mongodb+srv://AlexC:bermuda1@cluster0.vfw9k.mongodb.net/UtahPlanning?retryWrites=true&w=majority");
                dbClient = new MongoClient(settings);

                
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception {ex.GetType()} was created when connecting to the MongoDB and says {ex.Message}");
            }

            var database = dbClient.GetDatabase("UtahPlanning");
            var collection = database.GetCollection<BsonDocument>("PDF Data");

            // var database = dbClient.GetDatabase("UtahPlanning");


            string today = DateTime.Today.ToString();

            BsonDocument document = new BsonDocument()
                .Add("_id", BsonValue.Create(today))
                .AddRange(pdfList);

            
                
            
           /* document.Add(cityList, today);
            
            document.AddRange("cityList", new BsonArray(cityList.Select(i => i.ToBsonDocument())));*/
            
            //document = cityList.ToBsonDocument();
            collection.InsertOne(document);
        }

        // This is the method called from the Scrape Data button on the front end
        public static void ScrapeData()
        {
            ChromeDriver driver = new ChromeDriver();

            var html = GetHtml(driver);
            var CityList = ParseHtml(html);
            var pdfList = CycleAttributes(CityList, driver);

            Console.WriteLine();
            Console.ReadLine();

        }

        // generates front end
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        //Cycles through all city elements and pulls all nodes for planning commission files
        private static List<HtmlNode> CycleAttributes(List<HtmlNode> cityList, ChromeDriver driver)
        {
            int searchParam = cityList.Count;
            List<HtmlNode> pdfList = cityList;

            for (int i = 1; i < searchParam; i++)
            {
                IWebElement planningCommission;

                /*List<string> pdfData = new List<string>();*/
                Dictionary<string, string> pdfData = new Dictionary<string, string>();

                string city = cityList[i - 1].InnerText.ToString().Trim();
                
                // cycle by partial link text using the contects of the cityList
                IWebElement cityButton = driver.FindElementByPartialLinkText(city);
                cityButton.Click();

                System.Threading.Thread.Sleep(1000);

                //attempts to find a planning commission link within the box-fixed and writes the error if it cannot
                try
                {
                    planningCommission = driver.FindElementByPartialLinkText("Planning");
                }
                catch  (Exception ex)
                {
                    Console.WriteLine(ex);
                    continue;
                }

                planningCommission.Click();

                System.Threading.Thread.Sleep(3000);

                //creates an HTML document from the web page after navigation automation
                string html = driver.PageSource;
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

               //parses HTML document down to just nodes within the bottom table
                List<HtmlNode> tableElements = doc.DocumentNode.Descendants("a")
                    .Where(node => node.GetAttributeValue("href", "")
                   .Contains("pdf")).ToList();

                for (int j = 0; j < tableElements.Count; j++)
                {
                    pdfData.Add(tableElements[j].InnerHtml.ToString(), tableElements[j].ToString());
                }
                WriteList(pdfData);
                //
                //BUG, need to manage the data, because this is now a dictionary not a list, it will continue to overwrite
                //
            }
            return pdfList;
        }

        //This is the initial HTML pull that opens the website and returns a string of the html from this page
        private static string GetHtml(ChromeDriver driver)
        {
            var url = "https://www.utah.gov/pmn/index.html";


            driver.Navigate().GoToUrl(url);
            System.Threading.Thread.Sleep(1000);

            driver.FindElementByPartialLinkText("Cities").Click();
            System.Threading.Thread.Sleep(1000);

            string html = driver.PageSource;

            return html;

        }

        //This is the initial parse when handling multiple dynamic boxes
        private static List<HtmlNode> ParseHtml(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            // htmlDocument.LoadHtml(driver.PageSource);
            doc.Save("C:\\Users\\alexc\\Documents\\WorkPDFs\\TestHtml");

            var CityHtml = doc.DocumentNode.Descendants("ul")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("box-fixed")).ToList();

            var CityListItems = CityHtml[1].Descendants("li")
                .Where(node => node.GetAttributeValue("id", "")
                .Equals("")).ToList();

            return CityListItems;
        }

    }
}
