using Insuranceclaim.Models;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using System.Linq;

using System.Threading.Tasks;

using System;

using System.Collections.Generic;

namespace Insuranceclaim.Controllers

{

    [Route("Admin/AdminPolicies/[action]")]

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

            var policies = from p in _context.Policies select p;

            if (!string.IsNullOrEmpty(category) && category != "All")

            {

                policies = policies.Where(p => p.Category == category);

            }

            if (!string.IsNullOrEmpty(status) && status != "All")

            {

                policies = policies.Where(p => p.PolicyStatus.ToUpper() == status.ToUpper());

            }

            ViewBag.Categories = await _context.Policies.Select(p => p.Category).Distinct().ToListAsync();

            ViewBag.Statuses = new List<string> { "Active", "Inactive", "Cancelled" };

            ViewBag.SelectedCategory = category;

            ViewBag.SelectedStatus = status;

            return View("~/Views/Admin/AdminPolicies/Index.cshtml", await policies.ToListAsync());

        }

        // POST: AdminPolicies/Create

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("PolicyNumber,Category,PolicyName,CoverageAmount,AnnualPremium,PolicyStatus,Description")] Policy policy)

        {

            if (ModelState.IsValid)

            {

                policy.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

                _context.Add(policy);

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");

            }

            return RedirectToAction("Index");

        }

        // POST: AdminPolicies/ToggleStatus/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ToggleStatus(int id)

        {

            var policy = await _context.Policies.FindAsync(id);

            if (policy == null)

            {

                return NotFound();

            }

            policy.PolicyStatus = (policy.PolicyStatus?.ToUpper() == "ACTIVE") ? "INACTIVE" : "ACTIVE";

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");

        }

        // POST: AdminPolicies/Cancel/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Cancel(int id)

        {

            var policy = await _context.Policies.FindAsync(id);

            if (policy == null)

            {

                return NotFound();

            }

            policy.PolicyStatus = "CANCELLED";

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");

        }

        // POST: AdminPolicies/Delete/5

        [HttpPost, ActionName("Delete")]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)

        {

            var policy = await _context.Policies.FindAsync(id);

            if (policy != null)

            {

                _context.Policies.Remove(policy);

                await _context.SaveChangesAsync();

            }

            return RedirectToAction("Index");

        }

        // POST: AdminPolicies/Edit/5

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit([Bind("PolicyId,PolicyName,Category,CoverageAmount,AnnualPremium,Description")] Policy policy)

        {

            if (!ModelState.IsValid)

                return BadRequest(ModelState);

            var existingPolicy = await _context.Policies.FindAsync(policy.PolicyId);

            if (existingPolicy == null)

                return NotFound();

            existingPolicy.PolicyName = policy.PolicyName;

            existingPolicy.Category = policy.Category;

            existingPolicy.CoverageAmount = policy.CoverageAmount;

            existingPolicy.AnnualPremium = policy.AnnualPremium;

            existingPolicy.Description = policy.Description;

            _context.Update(existingPolicy);

            await _context.SaveChangesAsync();

            return Ok();

        }

        private bool PolicyExists(int id)

        {

            return _context.Policies.Any(e => e.PolicyId == id);

        }

    }

}

