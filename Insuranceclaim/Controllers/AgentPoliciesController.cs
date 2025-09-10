using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Insuranceclaim.Controllers
{
    public class AgentPoliciesController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public AgentPoliciesController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return RedirectToAction("AvailablePolicies");
        }

        [HttpGet]
        public IActionResult AvailablePolicies(int? userId)
        {
            // If a userId is provided (from Agents->Details), use it; otherwise fall back to current user's id
            int targetUserId;
            if (userId.HasValue)
            {
                targetUserId = userId.Value;
            }
            else
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out targetUserId))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            // Get all policies
            var allPolicies = _context.Policies.ToList();

            // Get applied policies for the target user
            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == targetUserId)
                .ToList();

            // Determine policy status based on AppliedPolicies.EnrollementStatus
            foreach (var policy in allPolicies)
            {
                var applied = appliedPolicies.FirstOrDefault(ap => ap.PolicyId == policy.PolicyId);
                if (applied != null)
                {
                    var status = (applied.EnrollementStatus ?? string.Empty).ToLower();
                    if (status == "enrolled" || status == "submitted for claim")
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

            // Pass the target user id to the view via ViewBag so client-side JS can include it in requests
            ViewBag.TargetUserId = targetUserId;

            return View("~/Views/Agent/AgentPolicies.cshtml", allPolicies);
        }

        [HttpGet]
        public IActionResult MyPolicies(int? userId)
        {
            int targetUserId = userId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get applied policies for the user
            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == targetUserId)
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

            ViewBag.TargetUserId = targetUserId;
            return View("~/Views/Agent/AgentMyPolicies.cshtml", enrolledPolicies);
        }

        [HttpGet]
        public IActionResult MyClaims(int? userId)
        {
            int targetUserId = userId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var myClaims = _context.Claims
                .Include(c => c.Policy)
                .Where(c => c.UserId == targetUserId)
                .ToList();

            ViewBag.TargetUserId = targetUserId;
            return View("~/Views/Agent/AgentClaims.cshtml", myClaims);
        }

        [HttpGet]
        public IActionResult Support(int? userId)
        {
            int targetUserId = userId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Load support tickets for current user
            var tickets = _context.SupportTickets
                .Where(s => s.UserId == targetUserId)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            ViewBag.TargetUserId = targetUserId;
            return View("~/Views/Agent/AgentSupport.cshtml", tickets);
        }

        [HttpPost]
        public IActionResult EnrollPolicy(int policyId, int? targetUserId)
        {
            // Determine which user the enrollment is for: provided targetUserId (agent acting for policyholder) or current user
            int userId;
            if (targetUserId.HasValue)
            {
                userId = targetUserId.Value;
            }
            else if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                return RedirectToAction("AvailablePolicies");
            }

            var isAlreadyEnrolled = _context.AppliedPolicies.Any(ap => ap.UserId == userId && ap.PolicyId == policyId && (ap.EnrollementStatus == "Enrolled" || ap.EnrollementStatus == "Submitted for claim"));
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

        [HttpPost]
        public IActionResult SubmitClaim(int policyId, decimal claimAmount, DateOnly incidentDate, string incidentDescription, IFormFile claimFile, int? targetUserId)
        {
            int userId;
            if (targetUserId.HasValue)
            {
                userId = targetUserId.Value;
            }
            else if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Policy not found.";
                return RedirectToAction("MyPolicies", "AgentPolicies");
            }

            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (appliedPolicy == null)
            {
                TempData["ErrorMessage"] = "No enrollment found for this policy.";
                return RedirectToAction("MyPolicies", "AgentPolicies");
            }

            // Minimum claim amount check
            if (claimAmount < 5000m)
            {
                TempData["ErrorMessage"] = "Minimum claim amount is 5,000.";
                return RedirectToAction("MyPolicies", "AgentPolicies");
            }

            // Ensure applied policy has remaining coverage
            var remainingCoverage = appliedPolicy.Remainingamount ?? policy.CoverageAmount ?? 0m;
            if (remainingCoverage <= 0)
            {
                TempData["ErrorMessage"] = "No remaining coverage available for this policy.";
                return RedirectToAction("MyPolicies", "AgentPolicies");
            }

            if (claimAmount > remainingCoverage)
            {
                TempData["ErrorMessage"] = "Coverage limit exceeded.";
                return RedirectToAction("MyPolicies", "AgentPolicies");
            }

            // Save claim
            var claim = new Insuranceclaim.Models.Claim
            {
                PolicyId = policyId,
                ClaimAmount = claimAmount,
                ClaimDate = DateOnly.FromDateTime(DateTime.Now),
                ClaimStatus = "Pending",
                UserId = userId,
                DescriptionofIncident = incidentDescription
            };
            _context.Claims.Add(claim);
            _context.SaveChanges();

            // Save document
            if (claimFile != null && claimFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                var fileName = Path.GetFileName(claimFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    claimFile.CopyTo(stream);
                }
                var document = new Document
                {
                    ClaimId = claim.ClaimId,
                    DocumentName = fileName,
                    DocumentPath = "/uploads/" + fileName,
                    DocumentType = Path.GetExtension(fileName)
                };
                _context.Documents.Add(document);
                _context.SaveChanges();
            }

            // Deduct claimed amount from applied policy remaining coverage
            appliedPolicy.Remainingamount = (appliedPolicy.Remainingamount ?? policy.CoverageAmount ?? 0m) - claimAmount;
            appliedPolicy.EnrollementStatus = "Submitted for claim";
            _context.AppliedPolicies.Update(appliedPolicy);

            // Do NOT update Policy.CoverageAmount

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("MyClaims", "AgentPolicies", new { userId = userId });
            //return RedirectToAction("AgentClaims");
        }

        [HttpPost]
        public IActionResult UpdateClaimStatus(int claimId, string status)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == claimId);
            if (claim == null) return NotFound();

            claim.ClaimStatus = status;
            _context.Claims.Update(claim);

            if (status == "Approved")
            {
                var applied = _context.AppliedPolicies.FirstOrDefault(ap => ap.PolicyId == claim.PolicyId && ap.UserId == claim.UserId);
                if (applied != null)
                {
                    applied.EnrollementStatus = "Claimed"; // allow re-enrollment
                    _context.AppliedPolicies.Update(applied);
                }
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSupportTicket([Bind("IssueDescription")] SupportTicket supportTicket, int? targetUserId)
        {
            if (!ModelState.IsValid)
            {
                // If model state is invalid, redirect back to support page
                TempData["ErrorMessage"] = "Please provide a valid issue description.";
                return RedirectToAction("Support", new { userId = targetUserId });
            }

            // Determine the user id this ticket is for
            int userId;
            if (targetUserId.HasValue)
            {
                userId = targetUserId.Value;
            }
            else
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out userId))
                {
                    // If user is not authenticated or ID is not a valid integer, redirect to login
                    return RedirectToAction("Login", "Account");
                }
            }

            // Set the UserId from the authenticated user or target
            supportTicket.UserId = userId;

            // Set default status and creation date
            supportTicket.TicketStatus = "Open";
            supportTicket.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

            _context.Add(supportTicket);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Support ticket submitted successfully!";

            // Redirect to the Support GET that returns the Policyholder support view with the updated tickets
            return RedirectToAction("Support", new { userId = userId });
        }
    }
}