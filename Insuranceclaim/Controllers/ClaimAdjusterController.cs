using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Insuranceclaim.Models;
using ClaimModel = Insuranceclaim.Models.Claim; // Namespace alias

namespace Insuranceclaim.Controllers
{
    public class ClaimAdjusterController : Controller
    {
        private readonly ClaimManagementSystemContext _context;

        public ClaimAdjusterController(ClaimManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Claim
        public async Task<IActionResult> Index()
        {
            var claimManagementSystemContext = _context.Claims
                .Include(c => c.Adjuster)
                .Include(c => c.Policy)
                .Include(c => c.User);
            return View(await claimManagementSystemContext.ToListAsync());
        }

        // GET: Claim/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .Include(c => c.Adjuster)
                .Include(c => c.Policy)
                .Include(c => c.Documents)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            ViewData["IsClaimProcessed"] = !string.IsNullOrEmpty(claim.AdjusterNotes) &&
                                           (claim.ClaimStatus == "Rejected" || claim.ClaimStatus == "pending admin review");

            return View(claim);
        }

        // POST: Claim/UpdateClaimDetails/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimDetails(int id, [Bind("ClaimId,AdjusterNotes,ClaimStatus")] ClaimModel updatedClaim)
        {
            if (id != updatedClaim.ClaimId)
            {
                return NotFound();
            }

            var claimToUpdate = await _context.Claims.FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claimToUpdate == null)
            {
                return NotFound();
            }

            // Check if the claim has already been processed by an adjuster
            if (claimToUpdate.AdjusterId.HasValue)
            {
                TempData["ErrorMessage"] = "This claim has already been processed.";
                return RedirectToAction(nameof(Details), new { id = claimToUpdate.ClaimId });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only the specific fields
                    claimToUpdate.AdjusterNotes = updatedClaim.AdjusterNotes;
                    claimToUpdate.ClaimStatus = updatedClaim.ClaimStatus;

                    // Get the logged-in adjuster's user ID from the claims
                    var loggedInUserIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (int.TryParse(loggedInUserIdString, out int loggedInUserId))
                    {
                        claimToUpdate.AdjusterId = loggedInUserId;
                    }
                    else
                    {
                        // Handle the case where the user ID is not available or not an integer
                        TempData["ErrorMessage"] = "Unable to identify the logged-in user.";
                        return RedirectToAction(nameof(Details), new { id = claimToUpdate.ClaimId });
                    }

                    claimToUpdate.AdjusterApprovalDate = DateTime.Today;

                    _context.Update(claimToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Changes saved successfully!";
                    return RedirectToAction(nameof(Details), new { id = claimToUpdate.ClaimId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimExists(updatedClaim.ClaimId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var claim = await _context.Claims
                .Include(c => c.Adjuster)
                .Include(c => c.Policy)
                .Include(c => c.Documents)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClaimId == id);

            ViewData["IsClaimProcessed"] = false;

            return View("Details", claim);
        }

        private bool ClaimExists(int id)
        {
            return _context.Claims.Any(e => e.ClaimId == id);
        }
    }
}
