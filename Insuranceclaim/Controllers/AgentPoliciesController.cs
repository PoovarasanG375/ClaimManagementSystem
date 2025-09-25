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
            int targetUserId = userId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var allPolicies = _context.Policies.ToList();
            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == targetUserId)
                .ToList();

            foreach (var policy in allPolicies)
            {
                // New logic: Check if the policy is marked INACTIVE
                var adminPolicy = _context.Policies.FirstOrDefault(p => p.PolicyId == policy.PolicyId);
                if (adminPolicy != null && (adminPolicy.PolicyStatus ?? string.Empty).ToLower() == "inactive")
                {
                    policy.PolicyStatus = "INACTIVE"; // Show as INACTIVE
                    continue; // Skip other checks as enrollment is not allowed
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

            ViewBag.TargetUserId = targetUserId;
            return View("~/Views/Agent/AgentPolicies.cshtml", allPolicies);
        }

        [HttpGet]
        public IActionResult MyPolicies(int? userId)
        {
            int targetUserId = userId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var appliedPolicies = _context.AppliedPolicies
                .Where(ap => ap.UserId == targetUserId)
                .ToList();

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

            var enrolledPolicies = appliedPolicies
                .Join(_context.Policies,
                    ap => ap.PolicyId,
                    p => p.PolicyId,
                    (ap, p) => new { Applied = ap, Policy = p })
                .Where(x => (x.Applied.Remainingamount ?? x.Policy.CoverageAmount ?? 0m) > 0)
                .Select(x => new Policy
                {
                    PolicyId = x.Policy.PolicyId,
                    PolicyNumber = x.Policy.PolicyNumber,
                    PolicyholderId = x.Policy.PolicyholderId,
                    CoverageAmount = x.Applied.Remainingamount ?? x.Policy.CoverageAmount,
                    PolicyStatus = x.Applied.EnrollementStatus,
                    CreatedDate = x.Applied.CreatedDate,
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

            var tickets = _context.SupportTickets
                .Where(s => s.UserId == targetUserId)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            ViewBag.TargetUserId = targetUserId;
            return View("~/Views/Agent/AgentSupport.cshtml", tickets);
        }
        // ... other methods
        [HttpGet]
        public IActionResult GetClaimDetails(int id, int? userId)
        {
            // Determine which user the details are for: the provided userId (agent acting for policyholder)
            // or the currently logged-in user if userId is not provided.
            int targetUserId;
            if (userId.HasValue)
            {
                targetUserId = userId.Value;
            }
            else if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out targetUserId))
            {
                return Unauthorized();
            }

            var claim = _context.Claims
                .Include(c => c.Policy)
                .Include(c => c.Adjuster)
                .FirstOrDefault(c => c.ClaimId == id && c.UserId == targetUserId);

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
        public IActionResult EnrollPolicy(int policyId, int? targetUserId)
        {
            int userId = targetUserId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                return RedirectToAction("AvailablePolicies", new { userId = userId });
            }

            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (appliedPolicy != null && (appliedPolicy.EnrollementStatus == "Enrolled" || appliedPolicy.EnrollementStatus == "Submitted for claim"))
            {
                return RedirectToAction("AvailablePolicies", new { userId = userId });
            }
            if (appliedPolicy != null && appliedPolicy.Remainingamount > 0)
            {
                return RedirectToAction("AvailablePolicies", new { userId = userId });
            }

            if (appliedPolicy != null)
            {
                appliedPolicy.EnrollementStatus = "Enrolled";
                appliedPolicy.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
                if (appliedPolicy.Remainingamount == null || appliedPolicy.Remainingamount <= 0)
                {
                    appliedPolicy.Remainingamount = policy.CoverageAmount;
                }
                _context.AppliedPolicies.Update(appliedPolicy);
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
            return RedirectToAction("MyPolicies", new { userId = userId });
        }

        [HttpPost]
        public IActionResult SubmitClaim(int policyId, decimal claimAmount, DateOnly incidentDate, string incidentDescription, IFormFile claimFile, int? targetUserId)
        {
            int userId = targetUserId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == policyId);
            if (policy == null)
            {
                TempData["ErrorMessage"] = "Policy not found.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (appliedPolicy == null)
            {
                TempData["ErrorMessage"] = "No enrollment found for this policy.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

            //if (appliedPolicy.CreatedDate >= DateOnly.FromDateTime(DateTime.Now))
            //{
            //    TempData["ErrorMessage"] = "You can only submit a claim from the day after your enrollment date.";
            //    return RedirectToAction("MyPolicies", new { userId = userId });
            //}

            if (appliedPolicy.CreatedDate.HasValue && incidentDate <= appliedPolicy.CreatedDate.Value)
            {
                TempData["ErrorMessage"] = "The incident date cannot be on or before your policy enrollment date.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

            if (claimAmount < 5000m)
            {
                TempData["ErrorMessage"] = "Minimum claim amount is 5,000.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

            var remainingCoverage = appliedPolicy.Remainingamount ?? policy.CoverageAmount ?? 0m;
            if (remainingCoverage <= 0)
            {
                TempData["ErrorMessage"] = "No remaining coverage available for this policy.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

            if (claimAmount > remainingCoverage)
            {
                TempData["ErrorMessage"] = "Coverage limit exceeded.";
                return RedirectToAction("MyPolicies", new { userId = userId });
            }

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

            appliedPolicy.Remainingamount = (appliedPolicy.Remainingamount ?? policy.CoverageAmount ?? 0m) - claimAmount;
            appliedPolicy.EnrollementStatus = "Submitted for claim";
            _context.AppliedPolicies.Update(appliedPolicy);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("MyClaims", new { userId = userId });
        }

        [HttpPost]
        public IActionResult ResubmitClaim(int claimId, int? targetUserId)
        {
            int userId = targetUserId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == claimId && c.UserId == userId);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("MyClaims", new { userId = userId });
            }

            if (!string.Equals(claim.ClaimStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only rejected claims can be restored for resubmission.";
                return RedirectToAction("MyClaims", new { userId = userId });
            }

            var applied = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == claim.PolicyId);
            var policy = _context.Policies.FirstOrDefault(p => p.PolicyId == claim.PolicyId);
            if (applied == null || policy == null)
            {
                TempData["ErrorMessage"] = "Policy enrollment not found.";
                return RedirectToAction("MyClaims", new { userId = userId });
            }

            applied.Remainingamount = (applied.Remainingamount ?? policy.CoverageAmount ?? 0m) + (claim.ClaimAmount ?? 0m);
            applied.EnrollementStatus = "Enrolled";
            _context.AppliedPolicies.Update(applied);

            claim.ClaimStatus = "Cancelled";
            _context.Claims.Update(claim);

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim amount restored to your policy. You can resubmit from My Policies.";
            return RedirectToAction("MyPolicies", new { userId = userId });
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
                    applied.EnrollementStatus = "Claimed";
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
                TempData["ErrorMessage"] = "Please provide a valid issue description.";
                return RedirectToAction("Support", new { userId = targetUserId });
            }

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
                    return RedirectToAction("Login", "Account");
                }
            }

            supportTicket.UserId = userId;
            supportTicket.TicketStatus = "Open";
            supportTicket.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

            _context.Add(supportTicket);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Support ticket submitted successfully!";

            return RedirectToAction("Support", new { userId = userId });
        }
    }
}