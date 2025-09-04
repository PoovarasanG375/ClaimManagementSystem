using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Insuranceclaim.Controllers
{
    [Route("Admin/AdminSupportTickets/[action]")]
    public class AdminSupportTicketsController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public AdminSupportTicketsController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        // GET: AdminSupportTickets
        public async Task<IActionResult> Index(string statusFilter)
        {
            var supportTicketsQuery = _context.SupportTickets
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                supportTicketsQuery = supportTicketsQuery.Where(s => s.TicketStatus == statusFilter);
            }

            var supportTickets = await supportTicketsQuery
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            return View("~/Views/Admin/AdminSupportTickets/Index.cshtml", supportTickets);
        }

        // GET: AdminSupportTickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supportTicket = await _context.SupportTickets
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.TicketId == id);

            if (supportTicket == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/AdminSupportTickets/Details.cshtml", supportTicket);
        }

        // POST: AdminSupportTickets/UpdateTicket - Handles both status and response updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTicket(int ticketId, string responseMessage)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId);

            if (ticket == null)
            {
                return NotFound();
            }

            // If a response is provided, automatically change the status to "Resolved"
            if (!string.IsNullOrEmpty(responseMessage))
            {
                ticket.Response = responseMessage;
                ticket.TicketStatus = "Resolved"; // Set status to Resolved
            }
            else
            {
                // If the response is cleared, the ticket should revert to "Open".
                // This is a good practice to handle cases where an admin clears a response.
                ticket.Response = null;
                ticket.TicketStatus = "Open";
            }

            _context.Update(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ticketId });
        }
    }
}
