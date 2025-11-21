using System.Security.Claims;

namespace POE.Models
{
    // File: Models/SupportingDocument.cs
    [Serializable]
public class SupportingDocument
{
    public int DocumentID { get; set; }
    public int ClaimID { get; set; } // Foreign Key back to the main Claim

    public string FileName { get; set; }
    public string FilePath { get; set; } // The path on the server where the file is saved
    public DateTime UploadDate { get; set; }

    // Navigation Property
    public virtual Claim Claim { get; set; }
}
}

