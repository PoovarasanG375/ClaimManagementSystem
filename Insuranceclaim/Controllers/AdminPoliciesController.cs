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

            var policies = _context.Policies.AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "All")

            {

                policies = policies.Where(p => p.Category == category);

            }

            if (!string.IsNullOrEmpty(status) && status != "All")

            {

                policies = policies.Where(p => p.PolicyStatus.ToUpper() == status.ToUpper());

            }

            // Order the policies by CreatedDate in descending order to show the most recent first

            var policyList = await policies.OrderByDescending(p => p.CreatedDate).ToListAsync();

            ViewBag.Categories = await _context.Policies.Select(p => p.Category).Distinct().ToListAsync();

            ViewBag.SelectedCategory = category;

            ViewBag.SelectedStatus = status;


            return View("~/Views/Admin/AdminPolicies/Index.cshtml", await policies.ToListAsync());

        }

        // POST: AdminPolicies/Create

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("Category,Type,PolicyName,CoverageAmount,AnnualPremium,Description")] Policy policy)

        {

            if (ModelState.IsValid)

            {

                // Automatically generate a sequential policy number

                policy.PolicyNumber = await GenerateNextPolicyNumber();

                // Set the default status and creation date

                policy.PolicyStatus = "ACTIVE";

                // Fix for the CS0029 error

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

        private async Task<string> GenerateNextPolicyNumber()

        {

            var lastPolicy = await _context.Policies.OrderByDescending(p => p.PolicyId).FirstOrDefaultAsync();

            if (lastPolicy != null && !string.IsNullOrEmpty(lastPolicy.PolicyNumber))

            {

                string lastNumberStr = lastPolicy.PolicyNumber.Replace("POL", "");

                if (int.TryParse(lastNumberStr, out int number))

                {

                    number++;

                    return $"POL{number:D2}";

                }

            }

            return "POL01";

        }

    }

}

