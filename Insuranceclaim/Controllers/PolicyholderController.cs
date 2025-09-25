using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

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

            // Determine policy status based on AppliedPolicies.EnrollementStatus and Policy.PolicyStatus
            foreach (var policy in allPolicies)
            {
                // New logic: Check if the policy is marked INACTIVE or CANCELLED
                var adminPolicy = _context.Policies.FirstOrDefault(p => p.PolicyId == policy.PolicyId);
                if (adminPolicy != null)
                {
                    string status = (adminPolicy.PolicyStatus ?? string.Empty).ToLower();
                    if (status == "inactive" || status == "cancelled")
                    {
                        policy.PolicyStatus = adminPolicy.PolicyStatus; // Use the exact status from the database
                        continue; // Skip other checks as enrollment is not allowed
                    }
                }

                var applied = appliedPolicies.FirstOrDefault(ap => ap.PolicyId == policy.PolicyId);
                if (applied != null)
                {
                    // Update: Allow re-enrollment only when remaining amount is 0.
                    if (applied.Remainingamount <= 0 && applied.EnrollementStatus == "Claimed")
                    {
                        policy.PolicyStatus = "Available"; // Allow re-enrollment
                    }
                    else if ((applied.EnrollementStatus ?? string.Empty).ToLower() == "enrolled" || (applied.EnrollementStatus ?? string.Empty).ToLower() == "submitted for claim" || (applied.EnrollementStatus ?? string.Empty).ToLower() == "claimed")
                    {
                        policy.PolicyStatus = "Enrolled"; // show Already Enrolled
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
             // Expire policies that are enrolled for more than 1 year and not claimed
            var today = DateOnly.FromDateTime(DateTime.Now);
            foreach (var ap in appliedPolicies)
            {
                if ((ap.EnrollementStatus == "Enrolled" || ap.EnrollementStatus == "enrolled") && ap.CreatedDate.HasValue)
                {
                    var enrolledDate = ap.CreatedDate.Value;
                    if (enrolledDate.AddYears(1) <= today)
                    {
                        ap.EnrollementStatus = "Expired";
                        _context.AppliedPolicies.Update(ap);
                    }
                }
            }
            _context.SaveChanges();

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
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var myClaims = _context.Claims
                .Include(c => c.Policy)
                .Where(c => c.UserId == userId)
                .ToList();

            return View("~/Views/Policyholder/MyClaims.cshtml", myClaims);
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

        [HttpGet]
        public IActionResult GetClaimDetails(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return Unauthorized();
            }

            var claim = _context.Claims
                .Include(c => c.Policy)
                .Include(c => c.Adjuster)
                .FirstOrDefault(c => c.ClaimId == id && c.UserId == userId);

            if (claim == null)
                return NotFound();

            var documents = _context.Documents
                .Where(d => d.ClaimId == id)
                .Select(d => new { d.DocumentId, d.DocumentName, d.DocumentPath })
                .ToList();

            return Json(new
            {
                claimId = claim.ClaimId,
                policyId = claim.PolicyId,
                policyName = claim.Policy?.PolicyName,
                claimAmount = claim.ClaimAmount,
                claimDate = claim.ClaimDate.HasValue ? claim.ClaimDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                status = claim.ClaimStatus,
                adjusterNotes = claim.AdjusterNotes,
                adminNotes = claim.AdminNotes,
                documents
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResubmitClaim(int claimId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == claimId && c.UserId == userId);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MyClaims");
            }

            if (!string.Equals(claim.ClaimStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only rejected claims can be restored for resubmission.";
                return RedirectToAction("MyClaims");
            }

            var applied = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == claim.PolicyId);
            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == claim.PolicyId);
            if (applied == null || policy == null)
            {
                TempData["ErrorMessage"] = "Policy enrollment not found.";
                return RedirectToAction("MyClaims");
            }

            // Add back the rejected claim amount to the remaining coverage so user can resubmit
            applied.Remainingamount = (applied.Remainingamount ?? policy.CoverageAmount ?? 0m) + (claim.ClaimAmount ?? 0m);
            applied.EnrollementStatus = "Enrolled";
            _context.AppliedPolicies.Update(applied);

            // Mark the claim as Cancelled so it won't be considered again
            claim.ClaimStatus = "Cancelled";
            _context.Claims.Update(claim);

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim amount restored to your policy. You can resubmit from My Policies.";
            return RedirectToAction("MyPolicies");
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

            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);

            // New logic: Check if the policy is currently "Enrolled" or "Submitted for claim" and has remaining coverage.
            // If so, redirect back as enrollment is not needed.
            if (appliedPolicy != null && (appliedPolicy.EnrollementStatus == "Enrolled" || appliedPolicy.EnrollementStatus == "Submitted for claim") && appliedPolicy.Remainingamount > 0)
            {
                TempData["ErrorMessage"] = "You are already enrolled in this policy.";
                return RedirectToAction("AvailablePolicies");
            }

            // This section handles both new enrollments and re-enrollments of Expired/Claimed policies.
            if (appliedPolicy != null)
            {
                // For existing applied policies that are "Expired" or "Claimed" with no remaining amount, update them.
                appliedPolicy.EnrollementStatus = "Enrolled";
                appliedPolicy.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
                appliedPolicy.Remainingamount = policy.CoverageAmount; // Reset to full coverage
                _context.AppliedPolicies.Update(appliedPolicy);
            }
            else
            {
                // For brand new enrollments (no AppliedPolicy record exists yet).
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
        public IActionResult SubmitClaim(int policyId, decimal claimAmount, DateOnly incidentDate, string incidentDescription, IFormFile claimFile)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Policy not found.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (appliedPolicy == null)
            {
                TempData["ErrorMessage"] = "No enrollment found for this policy.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            if (appliedPolicy.CreatedDate.HasValue && incidentDate <= appliedPolicy.CreatedDate.Value)
            {
                TempData["ErrorMessage"] = "The incident date cannot be on or before your policy enrollment date.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            // New: Check if the policy has been enrolled for at least one day before allowing a claim
            //if (appliedPolicy.CreatedDate >= DateOnly.FromDateTime(DateTime.Now))
            //{
            //    TempData["ErrorMessage"] = "You can only submit a claim from the day after your enrollment date.";
            //    return RedirectToAction("MyPolicies", "Policyholder");
            //}

            // Minimum claim amount check
            if (claimAmount < 5000m)
            {
                TempData["ErrorMessage"] = "Minimum claim amount is 5,000.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            // Ensure applied policy has remaining coverage
            var remainingCoverage = appliedPolicy.Remainingamount ?? policy.CoverageAmount ?? 0m;
            if (remainingCoverage <= 0)
            {
                TempData["ErrorMessage"] = "No remaining coverage available for this policy.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            if (claimAmount > remainingCoverage)
            {
                TempData["ErrorMessage"] = "Coverage limit exceeded.";
                return RedirectToAction("MyPolicies", "Policyholder");
            }

            // Save claim
            var claim = new Insuranceclaim.Models.Claim
            {
                PolicyId = policyId,
                ClaimAmount = claimAmount,
                ClaimDate = DateOnly.FromDateTime(DateTime.Now),
                IncidentDate = incidentDate,
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
            return RedirectToAction("MyClaims", "Policyholder");
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
        public IActionResult CreateSupportTicket([Bind("IssueDescription")] SupportTicket supportTicket)
        {
            if (!ModelState.IsValid)
            {
                // If model state is invalid, redirect back to support page
                TempData["ErrorMessage"] = "Please provide a valid issue description.";
                return RedirectToAction("Support", "Policyholder");
            }

            // Get the current user's ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                // If user is not authenticated or ID is not a valid integer, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Set the UserId from the authenticated user
            supportTicket.UserId = userId;

            // Set default status and creation date
            supportTicket.TicketStatus = "Open";
            supportTicket.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

            _context.Add(supportTicket);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Support ticket submitted successfully!";

            // Redirect to the Support GET that returns the Policyholder support view with the updated tickets
            return RedirectToAction("Support", "Policyholder");
        }
    }
}