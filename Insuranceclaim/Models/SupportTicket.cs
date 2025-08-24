using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class SupportTicket
{
    public int TicketId { get; set; }

    public int? UserId { get; set; }

    public string? IssueDescription { get; set; }

    public string? TicketStatus { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public virtual User? User { get; set; }
}
