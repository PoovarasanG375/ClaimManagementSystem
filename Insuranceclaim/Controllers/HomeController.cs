using Microsoft.AspNetCore.Mvc;


namespace Insuranceclaim.Models
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Benefits()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }

}