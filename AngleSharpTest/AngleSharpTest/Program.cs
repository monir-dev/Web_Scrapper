using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
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

        }

        public static async void GetAllBikeBdData()
        {
            // Get All bike Brands
            List<BikeBrands> bikeBrands = JsonConvert.DeserializeObject<List<BikeBrands>>(File.ReadAllText("BikeBrands.json"));

            // Get all bikes Url for each brand
            List<BikesUrl> bikesUrls = JsonConvert.DeserializeObject<List<BikesUrl>>(File.ReadAllText("bikesUrls.json"));

            List<Bike> bikesList = new List<Bike>();

            foreach (var bike in bikesUrls)
            {
                bikesList.Add(await GetBikeInfo(bike));
            }

            Console.Read();
        }

        private static async Task<Bike> GetBikeInfo(BikesUrl b)
        {
            // Get DOM
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(async req => req.Content(await GetContent(b.Url)));

            Bike bike = new Bike();
            bike.BrandId = b.BrandId;

            bike.Name = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.row > div.single_title > h4").First().InnerHtml;

            // Basic
            bike.Features = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(1) > table > tbody > tr").FirstOrDefault()?.ChildNodes[0].TextContent;
            bike.DisplacementCC = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(1) > table > tbody > tr").FirstOrDefault()?.ChildNodes[1].TextContent;
            bike.Mileage = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(1) > table > tbody > tr").FirstOrDefault()?.ChildNodes[2].TextContent;

            // Bike Overview
            bike.Price = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(1) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.FuelSupplySystem = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(2) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.StartingMethod = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(3) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.CoolingSystem = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(4) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.EngineeOilRecommendation = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(5) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TyresType = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(6) > td:nth-child(2)").FirstOrDefault()?.TextContent;
            bike.TopSpeed = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;

            // Specifications
            bike.TopSpeed = document.QuerySelectorAll("div.bikebd_posts_area >  div.post_item single_bikes > div.full_specifications > div:nth-child(2) > table > tbody > tr:nth-child(7) > td:nth-child(2)").FirstOrDefault()?.TextContent;


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
                var bikePost = document.QuerySelectorAll("article > div.entry-content > h2 > a");

                var href = ((IHtmlAnchorElement)bikePost.First()).Href;

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
        public string Name { get; set; }

        public string Image { get; set; }

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