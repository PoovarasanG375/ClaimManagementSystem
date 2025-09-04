using Microsoft.AspNetCore.Mvc;
using Insuranceclaim.Models;
using System.Linq;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

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
        var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Role.ToLower() == usertype.ToLower());

        if (user != null)
        {
            if (BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // Create claims for the user's identity
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
                };

                // Create a ClaimsIdentity and sign the user in
                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(claimsIdentity));

                switch (usertype.ToLower())
                {
                    case "admin":
                        return RedirectToAction("Index", "Admins");
                    case "agent":
                        return RedirectToAction("Index", "AgentUsers");
                    case "claim-adjuster":
                        return RedirectToAction("ClaimAdjusterHome", "ClaimAdjusterDashboard");
                    case "policy holder":
                        return RedirectToAction("PolicyHolderHome", "PolicyHolderDashboard");
                    default:
                        ViewBag.ErrorMessage = "Invalid user type.";
                        return View();
                }
            }
        }

        ViewBag.ErrorMessage = "Invalid username, password, or user type.";
        return View();
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

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Username = username,
            Password = hashedPassword,
            Role = userType,
            Email = Email
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        ViewBag.SuccessMessage = "Registration successful! You can now log in.";
        return View("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}