using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Insuranceclaim.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class AgentUsersController : Controller
{
    private readonly ClaimManagementSystemContext _context;

    public AgentUsersController(ClaimManagementSystemContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var currentAgentIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentAgentIdString))
        {
            // If the user is not authenticated, redirect to the login page.
            return RedirectToAction("Login", "Account");
        }

        var currentAgentId = int.Parse(currentAgentIdString);

        // Fetch users associated with the current agent.
        var myPolicyholders = await _context.Users
                                                .Where(u => u.AgentId == currentAgentId)
                                                .ToListAsync();

        // Pass the list of policyholders to the view.
        return View(myPolicyholders);
    }

    public async Task<IActionResult> ViewProfile(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var policyholder = await _context.Users
            .Include(u => u.Policies)
            .Include(u => u.Claims)
            .Include(u => u.SupportTickets)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (policyholder == null)
        {
            return NotFound();
        }

        var currentAgentIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentAgentIdString))
        {
            return Forbid();
        }
        var currentAgentId = int.Parse(currentAgentIdString);

        if (policyholder.AgentId != currentAgentId)
        {
            return Forbid();
        }

        return View(policyholder);
    }
}