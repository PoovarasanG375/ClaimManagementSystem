using Insuranceclaim.Models;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using System.Linq;

using System.Threading.Tasks;

using System;

namespace Insuranceclaim.Controllers

{

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

            // Apply filtering if a status is selected

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")

            {

                supportTicketsQuery = supportTicketsQuery.Where(s => s.TicketStatus == statusFilter);

            }

            // Order tickets by created date, latest first

            var supportTickets = await supportTicketsQuery

                .OrderByDescending(s => s.CreatedDate)

                .ToListAsync();

            ViewBag.StatusFilter = statusFilter; // Pass the current filter to the view

            return View(supportTickets);

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

            // Corrected Logic: Change status to "In Progress" only if it's "Open"

            if (supportTicket.TicketStatus == "Open")

            {

                supportTicket.TicketStatus = "In Progress";

                _context.Update(supportTicket);

                await _context.SaveChangesAsync();

            }

            return View(supportTicket);

        }

        // POST: AdminSupportTickets/UpdateStatus

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> UpdateStatus(int id, string newStatus)

        {

            var ticket = await _context.SupportTickets.FindAsync(id);

            if (ticket == null)

            {

                return NotFound();

            }

            ticket.TicketStatus = newStatus;

            _context.Update(ticket);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ticket.TicketId });

        }

        // POST: AdminSupportTickets/Respond

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Respond(int ticketId, string responseMessage)

        {

            var ticket = await _context.SupportTickets.FindAsync(ticketId);

            if (ticket == null)

            {

                return NotFound();

            }

            ticket.Response = responseMessage;

            ticket.TicketStatus = "Resolved"; // Automatically set to Resolved upon giving a response

            _context.Update(ticket);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ticketId });

        }

    }

}
