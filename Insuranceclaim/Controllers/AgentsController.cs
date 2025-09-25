using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Insuranceclaim.Models;

namespace Insuranceclaim.Controllers
{
    [Authorize(Roles = "Agent")]
    public class AgentsController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public AgentsController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Agents/Index (Policyholder Dashboard)
        public async Task<IActionResult> Index()
        {
            var agentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentIdStr))
            {
                return Unauthorized();
            }
            var agentId = int.Parse(agentIdStr);

            var policyholders = await _context.Users
                                                .Include(u => u.Agent)
                                                .Where(u => u.Role == "POLICY HOLDER" && u.AgentId == agentId)
                                                .ToListAsync();

            return View("~/Views/Agent/Agents.cshtml", policyholders);
        }

        // POST: Agents/Create (Handles adding a new Policyholder from the modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Email")] User policyholderUser)
        {
            if (ModelState.IsValid)
            {
                var agentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(agentIdStr))
                {
                    return Json(new { success = false, message = "User is not authorized." });
                }

                policyholderUser.Role = "POLICY HOLDER";
                policyholderUser.AgentId = int.Parse(agentIdStr);
                policyholderUser.Password = Guid.NewGuid().ToString();

                _context.Add(policyholderUser);
                await _context.SaveChangesAsync();

                var agent = await _context.Users.FindAsync(policyholderUser.AgentId);

                return Json(new
                {
                    success = true,
                    userId = policyholderUser.UserId,
                    username = policyholderUser.Username,
                    email = policyholderUser.Email,
                    agentUsername = agent?.Username
                });
            }

            return Json(new { success = false, message = "Invalid data." });
        }

        // GET: Agents/Details/5
        // Redirect to the AgentPolicies controller and include the selected policyholder id
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Redirect the agent to the management page, passing the policyholder's ID in the URL.
            return RedirectToAction("AvailablePolicies", "AgentPolicies", new { userId = id });
        }

        // POST: Agents/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var agentIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var agentId = int.Parse(agentIdStr);
            if (user.AgentId != agentId)
            {
                return Json(new { success = false, message = "Unauthorized action." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}