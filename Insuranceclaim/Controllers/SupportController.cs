using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Insuranceclaim.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Insuranceclaim.Controllers
{
    public class SupportController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public SupportController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Support()
        {
            // Get the current user's ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                // If user is not authenticated or ID is not a valid integer, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var supportTickets = await _context.SupportTickets
                                               .Where(t => t.UserId == userId)
                                               .OrderByDescending(t => t.CreatedDate)
                                               .ToListAsync();

            // Return the Policyholder/Support view with the model
            return View("~/Views/Policyholder/Support.cshtml", supportTickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupportTicket([Bind("IssueDescription")] SupportTicket supportTicket)
        {
            if (!ModelState.IsValid)
            {
                // If model state is invalid, redirect back to support page
                TempData["ErrorMessage"] = "Please provide a valid issue description.";
                return RedirectToAction("Support", "Support");
            }

            // Get the current user's ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                // If user is not authenticated or ID is not a valid integer, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Set the UserId from the authenticated user
            supportTicket.UserId = userId;

            // Set default status and creation date
            supportTicket.TicketStatus = "Open";
            supportTicket.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

            _context.Add(supportTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Support ticket submitted successfully!";

            // Redirect to the Support GET that returns the Policyholder support view with the updated tickets
            return RedirectToAction("Support", "Support");
        }
    }
}