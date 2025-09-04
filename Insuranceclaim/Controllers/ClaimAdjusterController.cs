using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Insuranceclaim.Models;

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
            var claimManagementSystemContext = _context.Claims.Include(c => c.Adjuster).Include(c => c.Policy);
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
                .FirstOrDefaultAsync(m => m.ClaimId == id);
            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // GET: Claim/Create
        public IActionResult Create()
        {
            ViewData["AdjusterId"] = new SelectList(_context.Users, "UserId", "UserId");
            ViewData["PolicyId"] = new SelectList(_context.Policies, "PolicyId", "PolicyId");
            return View();
        }

        // POST: Claim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClaimId,PolicyId,ClaimAmount,ClaimDate,ClaimStatus,AdjusterId,DescriptionOfClaim,AdjusterNotes,AdjusterApprovalDate,AdminNotes,AdminApprovalDate,FileName")] Claim claim)
        {
            if (ModelState.IsValid)
            {
                _context.Add(claim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AdjusterId"] = new SelectList(_context.Users, "UserId", "UserId", claim.AdjusterId);
            ViewData["PolicyId"] = new SelectList(_context.Policies, "PolicyId", "PolicyId", claim.PolicyId);
            return View(claim);
        }

        // GET: Claim/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }
            ViewData["AdjusterId"] = new SelectList(_context.Users, "UserId", "UserId", claim.AdjusterId);
            ViewData["PolicyId"] = new SelectList(_context.Policies, "PolicyId", "PolicyId", claim.PolicyId);
            return View(claim);
        }

        // POST: Claim/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClaimId,PolicyId,ClaimAmount,ClaimDate,ClaimStatus,AdjusterId,DescriptionOfClaim,AdjusterNotes,AdjusterApprovalDate,AdminNotes,AdminApprovalDate,FileName")] Claim claim)
        {
            if (id != claim.ClaimId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimExists(claim.ClaimId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AdjusterId"] = new SelectList(_context.Users, "UserId", "UserId", claim.AdjusterId);
            ViewData["PolicyId"] = new SelectList(_context.Policies, "PolicyId", "PolicyId", claim.PolicyId);
            return View(claim);
        }

        // GET: Claim/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .Include(c => c.Adjuster)
                .Include(c => c.Policy)
                .FirstOrDefaultAsync(m => m.ClaimId == id);
            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // POST: Claim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                _context.Claims.Remove(claim);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClaimExists(int id)
        {
            return _context.Claims.Any(e => e.ClaimId == id);
        }
    }
}
