using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Insuranceclaim.Models;

public partial class ClaimManagementSystemContext : DbContext
{
    public ClaimManagementSystemContext()
    {
    }

    public ClaimManagementSystemContext(DbContextOptions<ClaimManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppliedPolicy> AppliedPolicies { get; set; }

    public virtual DbSet<Claim> Claims { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LTIN650294;Database=ClaimManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppliedPolicy>(entity =>
        {
            entity.HasKey(e => e.ApplyId).HasName("PK__AppliedP__F0687FB1CD891A09");

            entity.Property(e => e.CreatedDate).HasColumnName("createdDate");
            entity.Property(e => e.EnrollementStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolicyId).HasColumnName("policyId");
            entity.Property(e => e.Remainingamount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Policy).WithMany(p => p.AppliedPolicies)
                .HasForeignKey(d => d.PolicyId)
                .HasConstraintName("FK__AppliedPo__polic__70DDC3D8");

            entity.HasOne(d => d.User).WithMany(p => p.AppliedPolicies)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AppliedPo__userI__6FE99F9F");
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(e => e.ClaimId).HasName("PK__Claim__01BDF9D39DE193A7");

            entity.ToTable("Claim");

            entity.Property(e => e.ClaimId).HasColumnName("claimId");
            entity.Property(e => e.AdjusterApprovalDate).HasColumnName("adjusterApprovalDate");
            entity.Property(e => e.AdjusterId).HasColumnName("adjusterId");
            entity.Property(e => e.AdjusterNotes).HasColumnName("adjusterNotes");
            entity.Property(e => e.AdminApprovalDate).HasColumnName("adminApprovalDate");
            entity.Property(e => e.AdminNotes).HasColumnName("adminNotes");
            entity.Property(e => e.ClaimAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("claimAmount");
            entity.Property(e => e.ClaimDate).HasColumnName("claimDate");
            entity.Property(e => e.ClaimStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("claimStatus");
            entity.Property(e => e.DescriptionofIncident)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PolicyId).HasColumnName("policyId");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Adjuster).WithMany(p => p.ClaimAdjusters)
                .HasForeignKey(d => d.AdjusterId)
                .HasConstraintName("FK__Claim__adjusterI__3E52440B");

            entity.HasOne(d => d.Policy).WithMany(p => p.Claims)
                .HasForeignKey(d => d.PolicyId)
                .HasConstraintName("FK__Claim__policyId__3D5E1FD2");

            entity.HasOne(d => d.User).WithMany(p => p.ClaimUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Claim__userId__72C60C4A");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Document__EFAAAD85C84B6C7B");

            entity.ToTable("Document");

            entity.Property(e => e.DocumentId).HasColumnName("documentId");
            entity.Property(e => e.ClaimId).HasColumnName("claimId");
            entity.Property(e => e.DocumentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("documentName");
            entity.Property(e => e.DocumentPath)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("documentPath");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("documentType");

            entity.HasOne(d => d.Claim).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ClaimId)
                .HasConstraintName("FK__Document__claimI__412EB0B6");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK__Policy__78E3A9220DBC707E");

            entity.ToTable("Policy");

            entity.Property(e => e.PolicyId).HasColumnName("policyId");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CoverageAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("coverageAmount");
            entity.Property(e => e.CreatedDate).HasColumnName("createdDate");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PolicyName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("policyName");
            entity.Property(e => e.PolicyNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("policyNumber");
            entity.Property(e => e.PolicyStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("policyStatus");
            entity.Property(e => e.PolicyholderId).HasColumnName("policyholderId");

            entity.HasOne(d => d.Policyholder).WithMany(p => p.Policies)
                .HasForeignKey(d => d.PolicyholderId)
                .HasConstraintName("FK__Policy__policyho__3A81B327");
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__SupportT__712CC6076AC675E9");

            entity.ToTable("SupportTicket");

            entity.Property(e => e.CreatedDate).HasColumnName("createdDate");
            entity.Property(e => e.IssueDescription)
                .HasColumnType("text")
                .HasColumnName("issueDescription");
            entity.Property(e => e.Response)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TicketStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ticketStatus");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.SupportTickets)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SupportTi__userI__440B1D61");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CFF4D0CF3EA");

            entity.ToTable("User");

            entity.HasIndex(e => e.Username, "UQ__User__F3DBC572473700B2").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasOne(d => d.Agent).WithMany(p => p.InverseAgent)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_User_Agent");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
