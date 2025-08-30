using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class Policy
{
    public int PolicyId { get; set; }

    public string? PolicyNumber { get; set; }

    public int? PolicyholderId { get; set; }

    public decimal? CoverageAmount { get; set; }

    public string? PolicyStatus { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public string? PolicyName { get; set; }

    public int? AnnualPremium { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<AppliedPolicy> AppliedPolicies { get; set; } = new List<AppliedPolicy>();

    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public virtual User? Policyholder { get; set; }
}
