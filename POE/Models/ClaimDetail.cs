using System.Security.Claims;

namespace POE.Models
{
   // File: Models/ClaimDetail.cs

public class ClaimDetail
{
    public int ClaimDetailID { get; set; }
    public int ClaimID { get; set; } // Foreign Key back to the main Claim

    public DateTime DateOfWork { get; set; }
    public string Description { get; set; }
    public decimal HoursWorked { get; set; }

    // Navigation Property
    public virtual Claim Claim { get; set; }
} 
}

