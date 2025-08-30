using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class AppliedPolicy
{
    public int? UserId { get; set; }

    public string? EnrollementStatus { get; set; }

    public int? PolicyId { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int ApplyId { get; set; }

    public virtual Policy? Policy { get; set; }

    public virtual User? User { get; set; }
}
