using Microsoft.AspNetCore.Mvc;
using Insuranceclaim.Models;
using System.Linq;

namespace Insuranceclaim.Controllers
{
    public class AccountController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public AccountController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // This method serves the login page
            return View();
        }

        [HttpPost]
        public IActionResult Login(string usertype, string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password && u.Role == usertype);
            if (user != null)
            {
                // Redirect based on user type
                switch (usertype.ToLower())
                {
                    case "admin":
                        return RedirectToAction("AdminHome", "AdminDashboard");
                    case "agent":
                        return RedirectToAction("AgentHome", "AgentDashboard");
                    case "claim-adjuster":
                        return RedirectToAction("ClaimAdjusterHome", "ClaimAdjusterDashboard");
                    case "policy holder":
                        return RedirectToAction("PolicyHolderHome", "PolicyHolderDashboard");
                    default:
                        ViewBag.ErrorMessage = "Invalid user type.";
                        return View();
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid username, password, or user type.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUp(string userType, string username, string password, string confirmPassword, string Email)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (_context.Users.Any(u => u.Email == Email))
            {
                ViewBag.ErrorMessage = "Email already exists.";
                return View();
            }

            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.ErrorMessage = "Username already exists.";
                return View();
            }

            var user = new User
            {
                Username = username,
                Password = password,
                Role = userType,
                Email = Email
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            ViewBag.SuccessMessage = "Registration successful! Please log in.";
            return View();
        }
    }
}
