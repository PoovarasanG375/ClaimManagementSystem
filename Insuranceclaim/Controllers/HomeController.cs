using Microsoft.AspNetCore.Mvc;


namespace Insuranceclaim.Controllers
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
        // update
        public IActionResult Contact()
        {
            return View();
        }
    }

}