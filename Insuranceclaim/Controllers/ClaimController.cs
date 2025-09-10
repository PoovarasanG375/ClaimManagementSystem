using Insuranceclaim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Insuranceclaim.Controllers
{

    public class UpdateClaimStatusModel
    {
        public int claimId { get; set; }
        public string status { get; set; }
        public string adminNotes { get; set; }

    }
    public class ClaimController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public ClaimController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateClaimStatus([FromBody] UpdateClaimStatusModel model)
        {
            // Check for null model and model validation
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data." });
            }

            try
            {
                // Fetch the claim from the database
                var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == model.claimId);

                // Check if the claim exists
                if (claim == null)
                {
                    return NotFound(new { success = false, message = "Claim not found." });
                }

                // Update the claim properties
                claim.ClaimStatus = model.status;
                claim.AdminNotes = model.adminNotes; // Use AdminNotes if that's the correct field
                claim.AdminApprovalDate = DateTime.Now;

                // Mark the claim as modified in the context
                _context.Claims.Update(claim);

                // Update the AppliedPolicy enrollment status if the claim is approved
                if (model.status == "Approved")
                {
                    var applied = _context.AppliedPolicies.FirstOrDefault(ap => ap.PolicyId == claim.PolicyId && ap.UserId == claim.UserId);
                    if (applied != null)
                    {
                        applied.EnrollementStatus = "Claimed";
                        _context.AppliedPolicies.Update(applied);
                    }
                }

                // Save all changes to the database
                _context.SaveChanges();

                // Return a successful response
                return Ok(new { success = true, message = "Claim status updated successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.Error.WriteLine($"Error updating claim status: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while updating the claim status." });
            }
        }
    }
}
