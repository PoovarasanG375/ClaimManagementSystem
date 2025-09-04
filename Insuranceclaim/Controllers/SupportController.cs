using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Insuranceclaim.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        return View(supportTickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupportTicket([Bind("IssueDescription")] SupportTicket supportTicket)
    {
        if (ModelState.IsValid)
        {
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
            return RedirectToAction(nameof(Support));
        }

        // If ModelState is not valid, return to the view with the model
        return View("Support", supportTicket);
    }
}