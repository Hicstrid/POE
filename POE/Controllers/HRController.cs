using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POE.Controllers;
using POE.Services; // Your DatabaseService namespace
using System.Data;
using System.Data.SqlClient;

namespace POE.Controllers
{
    public class HRController : Controller
    {
        private readonly DatabaseService _db;

        // Constructor with dependency injection
        public HRController(DatabaseService db)
        {
            _db = db;
        }

        // GET: /HR/Dashboard
        public IActionResult Dashboard()
        {
            // Check if user is HR (you might want to add HR role to your database)
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR" && userRole != "Academic Manager") // Temporary until you add HR role
            {
                TempData["Error"] = "Access denied. HR privileges required.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Get dashboard statistics
                var stats = new
                {
                    TotalLecturers = GetTotalLecturers(),
                    MonthlyClaims = GetMonthlyClaimCount(),
                    TotalAmount = GetMonthlyTotalAmount(),
                    PendingApprovals = GetPendingApprovalCount()
                };

                ViewBag.Stats = stats;
                return View();
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Failed to load dashboard: " + ex.Message;
                return View();
            }
        }

        // GET: /HR/LecturerManagement
        public IActionResult LecturerManagement()
        {
            var lecturers = new System.Collections.Generic.List<dynamic>();

            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_GetAllLecturers", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lecturers.Add(new
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                Name = reader["FirstName"] + " " + reader["LastName"],
                                Email = reader["Email"].ToString(),
                                TotalClaims = Convert.ToInt32(reader["TotalClaims"] ?? 0),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"] ?? 0),
                                JoinDate = Convert.ToDateTime(reader["JoinDate"] ?? DateTime.Now)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Failed to load lecturers: " + ex.Message;
            }

            return View(lecturers);
        }

        // GET: /HR/GenerateReport
        public IActionResult GenerateReport(string reportType = "monthly")
        {
            try
            {
                // Automated report data
                var reportData = new System.Collections.Generic.List<dynamic>();

                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_GetMonthlyReport", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ReportType", reportType);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportData.Add(new
                            {
                                Month = reader["Month"].ToString(),
                                Year = Convert.ToInt32(reader["Year"]),
                                TotalClaims = Convert.ToInt32(reader["TotalClaims"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                AverageClaim = Convert.ToDecimal(reader["AverageClaim"])
                            });
                        }
                    }
                }

                ViewBag.ReportType = reportType;
                ViewBag.ReportData = reportData;
                ViewBag.GeneratedDate = DateTime.Now;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Report generation failed: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // POST: /HR/BulkApproveClaims
        [HttpPost]
        public IActionResult BulkApproveClaims(int[] claimIds)
        {
            try
            {
                if (claimIds == null || claimIds.Length == 0)
                {
                    return Json(new { success = false, message = "No claims selected" });
                }

                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_BulkApproveClaims", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Convert array to comma-separated string for SQL
                    var idList = string.Join(",", claimIds);
                    cmd.Parameters.AddWithValue("@ClaimIds", idList);
                    cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("UserName"));
                    cmd.Parameters.AddWithValue("@ApprovalDate", DateTime.Now);

                    con.Open();
                    int affectedRows = cmd.ExecuteNonQuery();

                    return Json(new
                    {
                        success = true,
                        message = $"{affectedRows} claims approved successfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /HR/UpdateLecturer
        [HttpPost]
        public IActionResult UpdateLecturer(int userId, string email, string phone)
        {
            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_UpdateLecturer", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@UpdatedBy", HttpContext.Session.GetString("UserName"));

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Lecturer updated successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Private helper methods for dashboard statistics
        private int GetTotalLecturers()
        {
            using (var con = _db.GetConnection())
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Lecturer'", con))
            {
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private int GetMonthlyClaimCount()
        {
            using (var con = _db.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Claims WHERE MONTH(SubmissionDate) = MONTH(GETDATE()) AND YEAR(SubmissionDate) = YEAR(GETDATE())", con))
            {
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private decimal GetMonthlyTotalAmount()
        {
            using (var con = _db.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(SUM(TotalAmount), 0) FROM Claims WHERE MONTH(SubmissionDate) = MONTH(GETDATE()) AND YEAR(SubmissionDate) = YEAR(GETDATE())", con))
            {
                con.Open();
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private int GetPendingApprovalCount()
        {
            using (var con = _db.GetConnection())
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Claims WHERE Status = 'Pending'", con))
            {
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
-Add HRController with role-based access control
- Implement automated HR dashboard with real-time statistics
- Create lecturer management system with performance metrics
- Add bulk claim approval functionality for coordinators
- Implement automated monthly/quarterly reporting system
- Add HR-specific views and navigation