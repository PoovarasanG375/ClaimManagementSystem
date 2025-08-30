//using Insuranceclaim.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Claims;
//using Claim = System.Security.Claims.Claim;

//namespace Insuranceclaim.Controllers
//{
//    public class PolicyholderController : Controller
//    {
//        private readonly ClaimManagementSystemContext _context;

//        public PolicyholderController(ClaimManagementSystemContext context)
//        {
//            _context = context;
//        }

//        public IActionResult Dashboard()
//        {
//            // Redirect to the default dashboard page for policyholders
//            return RedirectToAction("AvailablePolicies");
//        }

//        [HttpGet]
//        public IActionResult AvailablePolicies()
//        {
//            // Get the integer UserId from the authenticated user's claims
//            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
//            {
//                // Handle case where user ID is not a valid integer
//                return RedirectToAction("Login", "Account");
//            }

//            // Get all policies from the Policies table
//            var allPolicies = _context.Policies.ToList();

//            // Get a list of policies the current user has already enrolled in from the AppliedPolicies table
//            var enrolledPolicyIds = _context.AppliedPolicies
//                                            .Where(ap => ap.UserId == userId)
//                                            .Select(ap => ap.PolicyId)
//                                            .ToList();

//            // Loop through all policies and update their status based on the enrolled policies list
//            foreach (var policy in allPolicies)
//            {
//                if (enrolledPolicyIds.Contains(policy.PolicyId))
//                {
//                    policy.PolicyStatus = "Enrolled";
//                }
//                else
//                {
//                    policy.PolicyStatus = "Available";
//                }
//            }
//            return View(allPolicies);
//        }

//        [HttpGet]
//        public IActionResult MyPolicies()
//        {
//            // Get the integer UserId from the authenticated user's claims
//            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            // Retrieve policies for the current user by joining through the AppliedPolicies table
//            var enrolledPolicies = _context.AppliedPolicies
//                                            .Include(ap => ap.Policy)
//                                            .Where(ap => ap.UserId == userId)
//                                            .Select(ap => new Policy
//                                            {
//                                                PolicyId = ap.Policy.PolicyId,
//                                                PolicyNumber = ap.Policy.PolicyNumber,
//                                                PolicyholderId = ap.UserId,
//                                                CoverageAmount = ap.Policy.CoverageAmount,
//                                                PolicyStatus = ap.EnrollementStatus, // Using EnrollmentStatus from AppliedPolicies table
//                                                CreatedDate = ap.CreatedDate, // Using CreatedDate from AppliedPolicies table
//                                                PolicyName = ap.Policy.PolicyName
//                                            })
//                                            .ToList();

//            return View(enrolledPolicies);
//        }

//        [HttpGet]
//        public IActionResult MyClaims()
//        {
//            // Get the integer UserId from the authenticated user's claims
//            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            // Logic to get claims submitted by the current user
//            var myClaims = _context.Claims.Where(c => c.Policy.PolicyholderId == userId).ToList();
//            return View(myClaims);
//        }

//        [HttpGet]
//        public IActionResult Support()
//        {
//            return View();
//        }

//        [HttpPost]
//        public IActionResult EnrollPolicy(int policyId)
//        {
//            // Get the integer UserId from the authenticated user's claims
//            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            // Check if the user is already enrolled to prevent duplicate entries
//            var isAlreadyEnrolled = _context.AppliedPolicies.Any(ap => ap.UserId == userId && ap.PolicyId == policyId);
//            if (isAlreadyEnrolled)
//            {
//                // Return to the AvailablePolicies page with a message or handle the error gracefully
//                return RedirectToAction("AvailablePolicies");
//            }

//            // Get a unique AppliedPolicyId. This assumes your database generates the ID.
//            // If your ID is not auto-generated, you'll need to handle it here.
//            var newAppliedPolicy = new AppliedPolicy
//            {
//                UserId = userId,
//                PolicyId = policyId,
//                EnrollementStatus = "Enrolled",
//                CreatedDate = DateOnly.FromDateTime(DateTime.Now)
//            };

//            _context.AppliedPolicies.Add(newAppliedPolicy);
//            _context.SaveChanges();

//            return RedirectToAction("MyPolicies");
//        }
//    }
//}


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

            var allPolicies = _context.Policies.ToList();
            var enrolledPolicyIds = _context.AppliedPolicies
                                            .Where(ap => ap.UserId == userId)
                                            .Select(ap => ap.PolicyId)
                                            .ToList();

            var viewModel = allPolicies.Select(p => new
            {
                Policy = p,
                IsEnrolled = enrolledPolicyIds.Contains(p.PolicyId)
            }).ToList();

            ViewBag.EnrolledPolicyIds = enrolledPolicyIds;
            return View(allPolicies);
        }

        [HttpGet]
        public IActionResult MyPolicies()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var myEnrolledPolicies = _context.AppliedPolicies
                                                .Where(ap => ap.UserId == userId)
                                                .Join(_context.Policies,
                                                      ap => ap.PolicyId,
                                                      p => p.PolicyId,
                                                      (ap, p) => new Policy
                                                      {
                                                          PolicyId = p.PolicyId,
                                                          PolicyNumber = p.PolicyNumber,
                                                          PolicyholderId = p.PolicyholderId,
                                                          CoverageAmount = p.CoverageAmount,
                                                          PolicyStatus = ap.EnrollementStatus, // Get status from AppliedPolicies
                                                          CreatedDate = ap.CreatedDate, // Get enrollment date from AppliedPolicies
                                                          PolicyName = p.PolicyName,
                                                      })
                                                .ToList();
            return View(myEnrolledPolicies);
        }

        [HttpGet]
        public IActionResult MyClaims()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var myClaims = _context.Claims
                                    .Where(c => c.Policy.PolicyholderId == userId) // Assuming PolicyholderId is linked to UserId
                                    .ToList();

            return View(myClaims);
        }

        [HttpGet]
        public IActionResult Support()
        {
            return View();
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