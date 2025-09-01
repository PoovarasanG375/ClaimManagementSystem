
using Insuranceclaim.Models;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using System.Linq;

using System.Threading.Tasks;

using System;

namespace Insuranceclaim.Controllers

{

    public class AdminPoliciesController : Controller

    {

        private readonly ClaimManagementSystemContext _context;

        public AdminPoliciesController(ClaimManagementSystemContext context)

        {

            _context = context;

        }

        // GET: AdminPolicies

        public async Task<IActionResult> Index(string category, string status)

        {

            var policies = from p in _context.Policies select p; // Corrected

            if (!string.IsNullOrEmpty(category) && category != "All")

            {

                policies = policies.Where(p => p.Category == category);

            }

            if (!string.IsNullOrEmpty(status) && status != "All")

            {

                policies = policies.Where(p => p.PolicyStatus.ToUpper() == status.ToUpper());

            }

            ViewBag.Categories = await _context.Policies.Select(p => p.Category).Distinct().ToListAsync(); // Corrected

            ViewBag.Statuses = await _context.Policies.Select(p => p.PolicyStatus).Distinct().ToListAsync(); // Corrected

            ViewBag.SelectedCategory = category;

            ViewBag.SelectedStatus = status;

            return View(await policies.ToListAsync());

        }

        // POST: AdminPolicies/Create

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("PolicyNumber,Category,PolicyName,CoverageAmount,Premium,PolicyStatus,Description")] Policy policy)

        {

            if (ModelState.IsValid)

            {

                policy.CreatedDate = DateOnly.FromDateTime(DateTime.Now); // Corrected

                _context.Add(policy);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));

            }

            return RedirectToAction(nameof(Index));

        }

        // POST: AdminPolicies/ToggleStatus/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ToggleStatus(int id)

        {

            var policy = await _context.Policies.FindAsync(id); // Corrected

            if (policy == null)

            {

                return NotFound();

            }

            policy.PolicyStatus = (policy.PolicyStatus.ToUpper() == "ACTIVE") ? "INACTIVE" : "ACTIVE";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }

        // POST: AdminPolicies/Delete/5

        [HttpPost, ActionName("Delete")]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)

        {

            var policy = await _context.Policies.FindAsync(id); // Corrected

            if (policy != null)

            {

                _context.Policies.Remove(policy); // Corrected

                await _context.SaveChangesAsync();

            }

            return RedirectToAction(nameof(Index));

        }

        // POST: AdminPolicies/Edit/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit([Bind("PolicyId,PolicyName,Category,CoverageAmount,Premium,Description")] Policy policy)

        {

            if (!ModelState.IsValid)

                return BadRequest(ModelState);

            var existingPolicy = await _context.Policies.FindAsync(policy.PolicyId); // Corrected

            if (existingPolicy == null)

                return NotFound();

            existingPolicy.PolicyName = policy.PolicyName;

            existingPolicy.Category = policy.Category;

            existingPolicy.CoverageAmount = policy.CoverageAmount;

            existingPolicy.Premium = policy.Premium;

            existingPolicy.Description = policy.Description;

            await _context.SaveChangesAsync();

            return Ok();

        }

        private bool PolicyExists(int id)

        {

            return _context.Policies.Any(e => e.PolicyId == id); // Corrected

        }

    }

}
