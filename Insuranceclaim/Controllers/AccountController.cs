using Insuranceclaim.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;

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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usertype, string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                var claims = new List<Claim>
{
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
     new Claim(ClaimTypes.Name, user.Username),
     new Claim(ClaimTypes.Role, user.Role)
};

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Keep the user logged in across requests
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                // Sign in the user, creating an authentication cookie
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                // Redirect based on user type
                switch (user.Role.ToLower())
                {
                    case "admin":
                        return RedirectToAction("Index", "Admins");
                    case "agent":
                        return RedirectToAction("Index", "Agents");
                    case "claim adjuster":
                        return RedirectToAction("Index", "ClaimAdjuster");
                    case "policy holder":
                        return RedirectToAction("Dashboard", "Policyholder");
                    default:
                        ViewBag.ErrorMessage = "Invalid user type.";
                        return View();
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid username, password.";
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
                Role = "POLICY HOLDER",
                Email = Email
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            ViewBag.SuccessMessage = "Registration successful! Please log in.";
            return View();
        }
    }
}
