namespace POE.Models
{

    public class ClaimSubmit
    {
        public decimal HourlyRate { get; set; }
        public string AdditionalNotes { get; set; }
        public IFormFile ClaimFile { get; set; }
        public List<ClaimDetail> Details { get; set; } = new List<ClaimDetail>();
    }
}