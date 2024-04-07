using Matisoft.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Matisoft.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _apiKey = "AIzaSyBBc-s9D9yxdbZxW_y2ggqiqgEcP5LFAKc";
        private readonly string _cx = "c0a1dc4bad20a495b";
        private readonly IConfiguration _configuration;

        
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GetImage(string query)
        {
            string url = $"https://www.googleapis.com/customsearch/v1?q={query}&cx={_cx}&searchType=image&key={_apiKey}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response
                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    var imageUrls = new List<string>();
                    foreach (var item in result.items)
                    {
                        imageUrls.Add(item.link.ToString());
                        if (imageUrls.Count == 4)
                            break;
                    }
                    if (imageUrls.Count < 5 || imageUrls.Count == 1)
                    {
                        string connectionString = _configuration.GetConnectionString("DefaultConnection");
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            for (int i = 0; i < imageUrls.Count; i++)
                            {
                                string insertQuery = $"INSERT INTO TblImage(ImageUrl)VALUES('{imageUrls[i]}')";
                                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            con.Close();
                        }
                    }
                    
                    return RedirectToAction("Index", imageUrls);
                }
                catch (HttpRequestException e)
                {
                    // Handle exceptions
                    return BadRequest(e.Message);
                }
            }
        }

        //return View();

        public IActionResult Privacy()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Select * From TblImage", con))
                {
                    var searchImg = new List<SearchingImage>();
                    //List<SelectListItem> listItems = new List<SelectListItem>();
                    cmd.CommandType = CommandType.Text;
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        searchImg.Add(new SearchingImage
                        {
                            Id = reader.GetInt32("Id"),
                            ImageUrl = reader.GetString("ImageUrl")
                        });
                    }
                    return View(searchImg);
                }
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
