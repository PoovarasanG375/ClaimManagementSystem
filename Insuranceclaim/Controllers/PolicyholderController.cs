using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;

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
            // Get applied policies for the user with status 'Enrolled' or 'Active'
            var enrolledPolicyIds = _context.AppliedPolicies
                .Where(ap => ap.UserId == userId && (ap.EnrollementStatus == "Enrolled" || ap.EnrollementStatus == "Active"))
                .Select(ap => ap.PolicyId)
                .ToList();

            // Mark enrolled policies
            foreach (var policy in allPolicies)
            {
                if (enrolledPolicyIds.Contains(policy.PolicyId))
                {
                    policy.PolicyStatus = "Enrolled";
                }
                else
                {
                    policy.PolicyStatus = "Available";
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
                .Where(ap => ap.UserId == userId && (ap.EnrollementStatus == "Enrolled" || ap.EnrollementStatus == "Active"))
                .ToList();

            // Get policy details for each applied policy
            var enrolledPolicies = appliedPolicies
                .Join(_context.Policies,
                      ap => ap.PolicyId,
                      p => p.PolicyId,
                      (ap, p) => new Policy
                      {
                          PolicyId = p.PolicyId,
                          PolicyNumber = p.PolicyNumber,
                          PolicyholderId = p.PolicyholderId,
                          CoverageAmount = p.CoverageAmount,
                          PolicyStatus = ap.EnrollementStatus,
                          CreatedDate = ap.CreatedDate, // Enrolled date
                          PolicyName = p.PolicyName,
                          AnnualPremium = p.AnnualPremium,
                          Description = p.Description
                      })
                .ToList();

            // Remove policies for which a claim has already been submitted
            var claimedPolicyIds = _context.Claims.Where(c => c.UserId == userId).Select(c => c.PolicyId).ToList();
            enrolledPolicies = enrolledPolicies.Where(p => !claimedPolicyIds.Contains(p.PolicyId)).ToList();

            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["SuccessMessage"] != null)
                ViewBag.SuccessMessage = TempData["SuccessMessage"];

            return View(enrolledPolicies);
        }

        [HttpGet]
        public IActionResult Support()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MyClaims()
        {
            // This method is now handled in ClaimController
            return RedirectToAction("MyClaims", "Claim");
        }

        [HttpPost]
        public IActionResult EnrollPolicy(int policyId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var isAlreadyEnrolled = _context.AppliedPolicies.Any(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (isAlreadyEnrolled)
            {
                return RedirectToAction("AvailablePolicies");
            }

            var newAppliedPolicy = new AppliedPolicy
            {
                UserId = userId,
                PolicyId = policyId,
                EnrollementStatus = "Enrolled", // A more descriptive status for the enrollment
                CreatedDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.AppliedPolicies.Add(newAppliedPolicy);
            _context.SaveChanges(); // This line was missing

            return RedirectToAction("MyPolicies");
        }
    }
}