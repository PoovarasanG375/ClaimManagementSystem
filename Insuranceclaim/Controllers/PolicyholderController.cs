using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;
using Microsoft.EntityFrameworkCore;

namespace Insuranceclaim.Controllers
{
    public class PolicyholderController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public PolicyholderController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return RedirectToAction("AvailablePolicies");
        }

        [HttpGet]
        public IActionResult AvailablePolicies()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all policies
            var allPolicies = _context.Policies.ToList();

            // Get applied policies for the user
            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == userId)
                .ToList();

            // Determine policy status based on AppliedPolicies.EnrollementStatus
            foreach (var policy in allPolicies)
            {
                var applied = appliedPolicies.FirstOrDefault(ap => ap.PolicyId == policy.PolicyId);
                if (applied != null)
                {
                    var status = (applied.EnrollementStatus ?? string.Empty).ToLower();
                    if (status == "enrolled" || status == "submittedforclaim")
                    {
                        policy.PolicyStatus = "Enrolled"; // show Already Enrolled
                    }
                    else if (status == "available")
                    {
                        policy.PolicyStatus = "Available"; // show Enroll Now
                    }
                    else if (status == "claimed")
                    {
                        policy.PolicyStatus = "Available"; // claimed -> can enroll again
                    }
                    else
                    {
                        policy.PolicyStatus = "Available";
                    }
                }
                else
                {
                    policy.PolicyStatus = "Available"; // no applied policy -> available to enroll
                }
            }

            return View(allPolicies);
        }

        [HttpGet]
        public IActionResult MyPolicies()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get applied policies for the user
            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == userId)
                .ToList();

            // Join applied policies with policy details and show Remainingamount as CoverageAmount
            var enrolledPolicies = appliedPolicies
                .Join(_context.Policies,
                      ap => ap.PolicyId,
                      p => p.PolicyId,
                      (ap, p) => new { Applied = ap, Policy = p })
                .Where(x => (x.Applied.Remainingamount ?? x.Policy.CoverageAmount ?? 0m) > 0) // exclude zero remaining from view
                .Select(x => new Policy
                {
                    PolicyId = x.Policy.PolicyId,
                    PolicyNumber = x.Policy.PolicyNumber,
                    PolicyholderId = x.Policy.PolicyholderId,
                    CoverageAmount = x.Applied.Remainingamount ?? x.Policy.CoverageAmount, // show remaining amount
                    PolicyStatus = x.Applied.EnrollementStatus,
                    CreatedDate = x.Applied.CreatedDate, // Enrolled date
                    PolicyName = x.Policy.PolicyName,
                    AnnualPremium = x.Policy.AnnualPremium,
                    Description = x.Policy.Description
                })
                .ToList();

            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["SuccessMessage"] != null)
                ViewBag.SuccessMessage = TempData["SuccessMessage"];

            return View(enrolledPolicies);
        }

        [HttpGet]
        public IActionResult MyClaims()
        {
            // This method is now handled in ClaimController
            return RedirectToAction("MyClaims", "Claim");
        }

        [HttpGet]
        public IActionResult Support()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Load support tickets for current user
            var tickets = _context.SupportTickets
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            return View(tickets);
        }

        [HttpPost]
        public IActionResult EnrollPolicy(int policyId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                return RedirectToAction("AvailablePolicies");
            }

            var isAlreadyEnrolled = _context.AppliedPolicies.Any(ap => ap.UserId == userId && ap.PolicyId == policyId && (ap.EnrollementStatus == "Enrolled" || ap.EnrollementStatus == "Submittedforclaim"));
            if (isAlreadyEnrolled)
            {
                return RedirectToAction("AvailablePolicies");
            }

            var existing = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (existing != null)
            {
                existing.EnrollementStatus = "Enrolled";
                existing.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
                // initialize Remainingamount if null
                if (existing.Remainingamount == null)
                {
                    existing.Remainingamount = policy.CoverageAmount;
                }
                _context.AppliedPolicies.Update(existing);
            }
            else
            {
                var newAppliedPolicy = new AppliedPolicy
                {
                    UserId = userId,
                    PolicyId = policyId,
                    EnrollementStatus = "Enrolled",
                    CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                    Remainingamount = policy.CoverageAmount
                };

                _context.AppliedPolicies.Add(newAppliedPolicy);
            }

            _context.SaveChanges();

            return RedirectToAction("MyPolicies");
        }
    }
}