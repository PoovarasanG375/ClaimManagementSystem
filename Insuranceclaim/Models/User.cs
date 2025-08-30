using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<AppliedPolicy> AppliedPolicies { get; set; } = new List<AppliedPolicy>();

    public virtual ICollection<Claim> ClaimAdjusters { get; set; } = new List<Claim>();

    public virtual ICollection<Claim> ClaimUsers { get; set; } = new List<Claim>();

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}
