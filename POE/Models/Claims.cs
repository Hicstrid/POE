
// File: Models/Claims.cs

using System;
using System.Collections.Generic; // Required for ICollection

namespace POE.Models
{
    public class Claims
    {
        // 1. Primary Key
        public int ClaimID { get; set; }

        // 2. Foreign Key (Relationship to the Lecturer who submitted the claim)
        public int UserID { get; set; }

        // 3. Claim Details (Input fields from the Lecturer's form)
        public DateTime SubmissionDate { get; set; }
        public decimal HourlyRate { get; set; } // The rate paid to the IC Lecturer
        public string AdditionalNotes { get; set; } // Any notes from the lecturer

        // 4. Calculated/Aggregated Fields (Derived from ClaimDetails)
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; } // HourlyRate * TotalHours

        // 5. Status Tracking (Part 2, Item 4)
        public string Status { get; set; } // e.g., "Pending", "Approved", "Rejected"
        public DateTime? ApprovalDate { get; set; } // Nullable, only set upon approval
        public string AdminNotes { get; set; } // Notes added by Coordinator/Manager

        // 6. Navigation Properties (Relationships)

        // Relationship 1: Back to the Lecturer (User who submitted the claim)
        public virtual User User { get; set; }

        // Relationship 2: To the detailed breakdown of hours worked
        // This links to the ClaimDetails table (1-to-Many relationship)
        public virtual ICollection<ClaimDetail> Details { get; set; }

        // Relationship 3: To the uploaded documents
        // This links to the SupportingDocuments table (1-to-Many relationship)
        public virtual ICollection<SupportingDocument> Documents { get; set; }
    }
}