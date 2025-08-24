using System;
using System.Collections.Generic;

namespace Insuranceclaim.Models;

public partial class Document
{
    public int DocumentId { get; set; }

    public int? ClaimId { get; set; }

    public string? DocumentName { get; set; }

    public string? DocumentPath { get; set; }

    public string? DocumentType { get; set; }

    public virtual Claim? Claim { get; set; }
}
