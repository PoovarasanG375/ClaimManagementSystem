using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class Claim
{
    public int ClaimId { get; set; }

    public int? PolicyId { get; set; }

    public decimal? ClaimAmount { get; set; }

    public DateOnly? ClaimDate { get; set; }

    public string? ClaimStatus { get; set; }

    public int? AdjusterId { get; set; }

    public DateTime? AdjusterApprovalDate { get; set; }

    public string? AdjusterNotes { get; set; }

    public DateTime? AdminApprovalDate { get; set; }

    public string? AdminNotes { get; set; }

    public int? UserId { get; set; }

    public virtual User? Adjuster { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Policy? Policy { get; set; }
}
