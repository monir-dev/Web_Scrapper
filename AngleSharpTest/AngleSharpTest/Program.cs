using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Newtonsoft.Json;
using RestSharp;

namespace AngleSharpTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //            Sharp();
            //StoreAllBikeBdDataToJson();
            GetAllBikeBdData();
        }

        public static async void StoreAllBikeBdDataToJson()
        {
            // Get All bike Brands
            var bikeBrands = await GetAllBrands();
            SaveAsJsonFile(JsonConvert.SerializeObject(bikeBrands), "BikeBrands");

            // Get all bikes Url for each brand
            List<BikesUrl> bikesUrls = new List<BikesUrl>();
            foreach (var brand in bikeBrands)
            {
                if (brand.Brand == "Avatar") continue;

                if (brand.Brand == "KTM" || brand.Brand == "Kiden" || brand.Brand == "Megelli" || brand.Brand == "Victor")
                {
                    var bikeUrl = await GetBikesUrl(brand.Url, true);

                    bikesUrls.Add(new BikesUrl()
                    {
                        BrandId = brand.Id,
                        Url = bikeUrl.First()
                    });

                    continue;
                }

                var bikes = await GetBikesUrlRecursively(brand.Url);
                foreach (var bike in bikes)
                {
                    bikesUrls.Add(new BikesUrl()
                    {
                        BrandId = brand.Id,
                        Url = bike
                    });
                }
            }
            SaveAsJsonFile(JsonConvert.SerializeObject(bikesUrls), "bikesUrls");


            // Get All bikes from bikes Url
            List<Bike> bikeLists = new List<Bike>();
            foreach (var bike in bikesUrls)
            {
                bikeLists.Add(await GetBikeInfo(bike));
            }
            SaveAsJsonFile(JsonConvert.SerializeObject(bikeLists), "bikeLists");
        }

        public static async void GetAllBikeBdData()
        {
            // Get All bike Brands
            //List<BikeBrands> bikeBrands = JsonConvert.DeserializeObject<List<BikeBrands>>(File.ReadAllText("BikeBrands.json"));

            // Get all bikes Url for each brand
            //List<BikesUrl> bikesUrls = JsonConvert.DeserializeObject<List<BikesUrl>>(File.ReadAllText("bikesUrls.json"));

            // Get All bikes
            List<BikesUrl> bikeLists = JsonConvert.DeserializeObject<List<BikesUrl>>(File.ReadAllText("bikeLists.json"));


            

            Console.Read();
        }

        private static async Task<Bike> GetBikeInfo(BikesUrl b)
        {
            // Get DOM
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(async req => req.Content(await GetContent("https://www.bikebd.com/bikes/hero-achiever-150/")));
            //var document = await context.OpenAsync(async req => req.Content(await GetContent(b.Url)));

            Bike bike = new Bike();
            bike.BrandId = b.BrandId;

            bike.PostTitle = document.Title;

            var imageOwls = document
                .QuerySelectorAll(
                    "body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.single_post_thumb")
                .FirstOrDefault()?.ChildNodes[0].ChildNodes;


            List<string> bikeImages = new List<string>();

            foreach (var img in imageOwls)
            {
                var imageDiv = img.ToHtml();

                string src = Regex.Match(imageDiv, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase).Groups[1].Value;
                bikeImages.Add(src);
            }

            bike.Images = bikeImages.ToArray();


            bike.Name = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(1) > div.col-sm-9 > div > h4").FirstOrDefault()?.TextContent;

            // Basic
            bike.Features = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(1) > table > tbody > tr > td:nth-child(1)").FirstOrDefault()?.TextContent;
            bike.DisplacementCC = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(1) > table > tbody > tr > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Mileage = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(1) > table > tbody > tr > td:nth-child(3)").FirstOrDefault()?.TextContent;

            // Bike Overview
            bike.Price = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.FuelSupplySystem = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.StartingMethod = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.CoolingSystem = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.EngineeOilRecommendation = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TyresType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(6) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TopSpeed = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            // Specifications
            bike.EngineeType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.MaximumPower = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.MaximumTorque = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Bore = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Stroke = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.CompressionRatio = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(6) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.NoOfCylinders = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(5) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            // Transmission
            bike.TransmissionType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(6) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.NoOfGears = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(6) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.ClutchType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(6) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;


            // Chassis & Suspension
            bike.ChassisType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(7) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.FrontSuspension = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(7) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.RearSuspension = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(7) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;


            // Brakes
            bike.FrontBrakeType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(8) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.RearBrakeType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(8) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.FrontBrakeDiameter = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(8) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.RearBrakeDiameter = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(8) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.AntiLockBrakingSystem_ABS = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(8) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;


            // Wheels & Tires
            bike.FrontTireSize = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(9) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.RearTireSize = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(9) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TubelessTires = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(9) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;


            // Dimensions
            bike.OverallLength = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.OverallWidth = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.OverallHeight = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.GroundClearance = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Weight = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.FuelTankCapacity = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(6) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Wheelbase = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(10) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            // Electricals
            bike.BatteryType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(11) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.BatteryVoltage = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(11) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.HeadLight = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(11) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TailLight = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(11) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Indicators = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(11) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            // Features
            bike.Speedometer = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.Odometer = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.RPMMeter = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.HandleType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.SeatType = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.PassengerGrabRail = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(6) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.EngineKillSwitch = document.QuerySelectorAll("body > div.full-width > section.bikebd_main_content_area > div > div > div.col-sm-7 > div.bikebd_posts_area > div > div:nth-child(12) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            return bike;
        }

        private static void SaveAsJsonFile(string Content, string FileName)
        {
            try
            {
                File.WriteAllText($"{FileName}.json", Content);
                Console.WriteLine($"{FileName} saved");
            }
            catch  { }
        }

        private static async Task<List<string>> GetBikesUrlRecursively(string url)
        {
            var bikeUrls = new List<string>();
            int page = 1;

            var hasBikes = true;
            while (hasBikes)
            {
                if (page > 1)
                {
                    var urlSlices = url.Split('/');
                    url = $"{urlSlices[0]}//{urlSlices[2]}/page/{page}/{urlSlices[3]}";
                }

                List<string> bikes = await GetBikesUrl(url);

                if (bikes == null) break;

                bikeUrls.AddRange(bikes);

                // check pagination
                if (bikes.Count() >= 10)
                {
                    page += 1;
                }
                else
                {
                    hasBikes = false;
                    page = 0;
                }
            }

            return bikeUrls;
        }

        private static async Task<List<string>> GetBikesUrl(string url, bool Single = false)
        {
            // Get DOM
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(async req => req.Content(await GetContent(url)));

            var bikesUrls = new List<string>();


            if (Single)
            {
                var bikePost = document.QuerySelectorAll("div.entry-content > h2").FirstOrDefault()?.ToHtml();

                string href = Regex.Match(bikePost, "<a.+?href=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase).Groups[1].Value;
                //var href = ((IHtmlAnchorElement)bikePost)?.Href;

                bikesUrls.Add(href);

                return bikesUrls;
            }

            var bikePosts = document.QuerySelectorAll("body > div.full-width > section > div > div > div.col-sm-7 > div.bikebd_posts_area > div");

            var notFound = bikePosts[1].TextContent.Contains("Sorry, Requested Bike Not Found.....! Please try again");

            // if no selected element
            if (bikePosts == null || notFound) return null;


            var i = 0;
            foreach (var item in bikePosts)
            {
                if (i == 0)
                {
                    i++;
                    continue;
                }

                var href = ((IHtmlAnchorElement)item.FirstChild.FirstChild.ChildNodes[1]).Href;

                bikesUrls.Add(href);
            }

            return bikesUrls;
        }

        static async Task<List<BikeBrands>> GetAllBrands()
        {
            //Create a new context for evaluating webpages with the default config
            var context = BrowsingContext.New(Configuration.Default);

            //Create a document from a virtual request / response pattern
            var document = await context.OpenAsync(async req => req.Content(await GetContent("https://www.bikebd.com/")));

            var brands = document.QuerySelectorAll("div.left_ad_area > div.textwidget > ul > li > a");

            var bikeBrands = new List<BikeBrands>();

            var i = 1;
            foreach (var b in brands)
            {
                bikeBrands.Add(new BikeBrands()
                {
                    Id = i,
                    Brand = b.TextContent,
                    Url = ((IHtmlAnchorElement)b).Href
                });

                i++;
            }

            return bikeBrands;
        }


        static async Task<string> GetContent(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Accept", "*/*");
            var response = client.Execute(request).Content;

            return response;
        }
    }


    public class BikesUrl
    {
        public int Id { get; set; }
        public int BrandId { get; set; }
        public string Url { get; set; }
    }


    public class Bike
    {
        public int Id { get; set; }
        public int BrandId { get; set; }

        public string PostTitle { get; set; }

        public string Name { get; set; }

        public string[] Images { get; set; }

        // Basic
        public string Features { get; set; }
        public string DisplacementCC { get; set; }
        public string Mileage { get; set; }

        // Bike Overview
        public string Price { get; set; }
        public string FuelSupplySystem { get; set; }
        public string StartingMethod { get; set; }
        public string CoolingSystem { get; set; }
        public string EngineeOilRecommendation { get; set; }
        public string TyresType { get; set; }
        public string TopSpeed { get; set; }

        // Specifications
        public string EngineeType { get; set; }
        public string MaximumPower { get; set; }
        public string MaximumTorque { get; set; }
        public string Bore { get; set; }
        public string Stroke { get; set; }
        public string CompressionRatio { get; set; }
        public string NoOfCylinders { get; set; }

        // Transmission
        public string TransmissionType { get; set; }
        public string NoOfGears { get; set; }
        public string ClutchType { get; set; }

        // Chassis & Suspension
        public string ChassisType { get; set; }
        public string FrontSuspension { get; set; }
        public string RearSuspension { get; set; }

        // Brakes
        public string FrontBrakeType { get; set; }
        public string RearBrakeType { get; set; }
        public string FrontBrakeDiameter { get; set; }
        public string RearBrakeDiameter { get; set; }
        public string AntiLockBrakingSystem_ABS { get; set; }

        // Wheels & Tires
        public string FrontTireSize { get; set; }
        public string RearTireSize { get; set; }
        public string TubelessTires { get; set; }

        // Dimensions
        public string OverallLength { get; set; }
        public string OverallWidth { get; set; }
        public string OverallHeight { get; set; }
        public string GroundClearance	 { get; set; }
        public string Weight { get; set; }
        public string FuelTankCapacity { get; set; }
        public string Wheelbase { get; set; }

        // Electricals
        public string BatteryType { get; set; }
        public string BatteryVoltage { get; set; }
        public string HeadLight { get; set; }
        public string TailLight { get; set; }
        public string Indicators { get; set; }

        // Features
        public string Speedometer { get; set; }
        public string Odometer { get; set; }
        public string RPMMeter { get; set; }
        public string HandleType { get; set; }
        public string SeatType { get; set; }
        public string PassengerGrabRail { get; set; }
        public string EngineKillSwitch { get; set; }
    }


    public class BikeBrands
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Url { get; set; }
    }
}