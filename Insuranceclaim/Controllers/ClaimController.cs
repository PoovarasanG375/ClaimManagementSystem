using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Insuranceclaim.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public ClaimController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult SubmitClaim(int policyId, decimal claimAmount, DateOnly incidentDate, string incidentDescription, IFormFile claimFile)
        {
            if (!int.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out int userId))
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

            // Minimum claim amount check
            if (claimAmount < 5000m)
            {
                TempData["ErrorMessage"] = "Minimum claim amount is ?5,000.";
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
            appliedPolicy.EnrollementStatus = "Submittedforclaim";
            _context.AppliedPolicies.Update(appliedPolicy);

            // Do NOT update Policy.CoverageAmount

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("MyClaims", "Claim");
        }

        [HttpGet]
        public IActionResult MyClaims()
        {
            if (!int.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var myClaims = _context.Claims
                .Include(c => c.Policy)
                .Where(c => c.UserId == userId)
                .ToList();

            return View("~/Views/Policyholder/MyClaims.cshtml", myClaims);
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
    }
}
