using Insuranceclaim.Models;

using Microsoft.AspNetCore.Mvc;

using System.Linq;

using System.Threading.Tasks;

public class AdminsController : Controller

{

    private readonly ClaimManagementSystemContext _context;

    public AdminsController(ClaimManagementSystemContext context)

    {

        _context = context;

    }

    public async Task<IActionResult> Index()

    {

        // Fetch data from the database

        var totalPolicies = _context.Policies.Count();

        // Corrected line to count claims based on the 'Pending_Admin_Review' status

        var pendingClaims = _context.Claims.Count(c => c.ClaimStatus == "pending admin review");

        // Corrected line: Count all users where UserRole is NOT "Admin"

        var activeUsers = _context.Users.Count(u => u.Role != "Admin");

        var openTickets = _context.SupportTickets.Count(t => t.TicketStatus == "Open");

        // Pass the data to the view using ViewBag

        ViewBag.TotalPolicies = totalPolicies;

        ViewBag.PendingClaims = pendingClaims;

        ViewBag.ActiveUsers = activeUsers;

        ViewBag.OpenTickets = openTickets;

        return View("~/Views/Admin/Admins/Index.cshtml");

    }

}
