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

            if (claimAmount > (policy.CoverageAmount ?? 0))
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

            // Remove from MyPolicies (delete from AppliedPolicies)
            var appliedPolicy = _context.AppliedPolicies.FirstOrDefault(ap => ap.UserId == userId && ap.PolicyId == policyId);
            if (appliedPolicy != null)
            {
                _context.AppliedPolicies.Remove(appliedPolicy);
                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("MyClaims");
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
    }
}
