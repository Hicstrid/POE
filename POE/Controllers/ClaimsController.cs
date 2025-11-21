// File: Controllers/ClaimsController.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POE.Models;
using POE.Services;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace POE.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly DatabaseService _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ClaimsController(DatabaseService db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
        }

        // -------------------------------
        // LECTURER ACTIONS
        // -------------------------------

        // GET: /Claims/SubmitClaim - FIXED: Now uses ClaimSubmit model
        public IActionResult SubmitClaim()
        {
            // Initialize with one empty detail for the form
            var model = new ClaimSubmit
            {
                Details = new System.Collections.Generic.List<ClaimDetail> { new ClaimDetail() }
            };
            return View(model);
        }

        // POST: /Claims/Submit
        [HttpPost]
        public async Task<IActionResult> Submit(ClaimSubmit model)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0) return RedirectToAction("Index", "Home"); // Not logged in

                // FIXED: Added null check for Details
                decimal totalHours = model.Details?.Sum(d => d.HoursWorked) ?? 0;

                int claimId;

                // 1️⃣ Save claim to DB
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_SubmitClaim", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Month", DateTime.Now.ToString("MMMM"));
                    cmd.Parameters.AddWithValue("@Year", DateTime.Now.Year);
                    cmd.Parameters.AddWithValue("@HoursWorked", totalHours);
                    cmd.Parameters.AddWithValue("@HourlyRate", model.HourlyRate);
                    cmd.Parameters.AddWithValue("@AdditionalNotes", model.AdditionalNotes ?? "");

                    con.Open();
                    cmd.ExecuteNonQuery();

                    // Get the last inserted ClaimId
                    cmd.CommandText = "SELECT SCOPE_IDENTITY()";
                    cmd.CommandType = System.Data.CommandType.Text;
                    claimId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2️⃣ Save claim details to DB
                if (model.Details != null && model.Details.Any())
                {
                    foreach (var detail in model.Details)
                    {
                        using (var con = _db.GetConnection())
                        using (var cmd = new SqlCommand("SP_AddClaimDetail", con))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ClaimId", claimId);
                            cmd.Parameters.AddWithValue("@DateOfWork", detail.DateOfWork);
                            cmd.Parameters.AddWithValue("@Description", detail.Description);
                            cmd.Parameters.AddWithValue("@HoursWorked", detail.HoursWorked);

                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // 3️⃣ Handle file upload
                if (model.ClaimFile != null && model.ClaimFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                    var fileExtension = Path.GetExtension(model.ClaimFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ViewData["Error"] = "Invalid file type. Please upload PDF, DOCX, or XLSX files only.";
                        return View("SubmitClaim", model);
                    }

                    // Validate file size (5MB limit)
                    if (model.ClaimFile.Length > 5 * 1024 * 1024)
                    {
                        ViewData["Error"] = "File size too large. Maximum size is 5MB.";
                        return View("SubmitClaim", model);
                    }

                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "documents/claims");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid() + "_" + model.ClaimFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ClaimFile.CopyToAsync(fs);
                    }

                    // Save document info to DB
                    using (var con = _db.GetConnection())
                    using (var cmd = new SqlCommand("SP_UploadDocument", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClaimId", claimId);
                        cmd.Parameters.AddWithValue("@FileName", model.ClaimFile.FileName);
                        cmd.Parameters.AddWithValue("@FileType", fileExtension);
                        cmd.Parameters.AddWithValue("@FilePath", uniqueFileName);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim submitted successfully!";
                return RedirectToAction("Tracking");
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Failed to submit claim: " + ex.Message;
                return View("SubmitClaim", model);
            }
        }

        // GET: /Claims/Tracking
        public IActionResult Tracking()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Index", "Home"); // Not logged in

            var claims = new System.Collections.Generic.List<Claims>();

            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_GetUserClaims", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            claims.Add(new Claims
                            {
                                ClaimID = Convert.ToInt32(reader["ClaimId"]),
                                UserID = userId,
                                TotalHours = Convert.ToDecimal(reader["HoursWorked"]),
                                HourlyRate = Convert.ToDecimal(reader["HourlyRate"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"].ToString(),
                                SubmissionDate = Convert.ToDateTime(reader["DateSubmitted"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Failed to load claims: " + ex.Message;
            }

            return View(claims);
        }

        // -------------------------------
        // ADMIN ACTIONS
        // -------------------------------

        // GET: /Claims/AdminApproval
        public IActionResult AdminApproval()
        {
            // Check if user is admin/coordinator
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || (userRole != "Programme Coordinator" && userRole != "Academic Manager"))
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var claims = new System.Collections.Generic.List<dynamic>();

            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_GetAllClaims", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            claims.Add(new
                            {
                                ClaimId = Convert.ToInt32(reader["ClaimId"]),
                                LecturerName = reader["FirstName"] + " " + reader["LastName"],
                                Month = reader["Month"].ToString(),
                                Year = Convert.ToInt32(reader["Year"]),
                                HoursWorked = Convert.ToDecimal(reader["HoursWorked"]),
                                HourlyRate = Convert.ToDecimal(reader["HourlyRate"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"].ToString(),
                                DateSubmitted = Convert.ToDateTime(reader["DateSubmitted"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Failed to load pending claims: " + ex.Message;
            }

            return View(claims);
        }

        // POST: /Claims/Approve
        [HttpPost]
        public IActionResult Approve(int claimId)
        {
            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_UpdateClaimStatus", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClaimId", claimId);
                    cmd.Parameters.AddWithValue("@Status", "Approved");
                    cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("UserName"));
                    cmd.Parameters.AddWithValue("@ApprovalDate", DateTime.Now);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to approve claim: " + ex.Message;
            }

            return RedirectToAction("AdminApproval");
        }

        // POST: /Claims/Reject
        [HttpPost]
        public IActionResult Reject(int claimId, string rejectionNotes)
        {
            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_UpdateClaimStatus", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClaimId", claimId);
                    cmd.Parameters.AddWithValue("@Status", "Rejected");
                    cmd.Parameters.AddWithValue("@AdminNotes", rejectionNotes ?? "");
                    cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("UserName"));

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to reject claim: " + ex.Message;
            }

            return RedirectToAction("AdminApproval");
        }

        // GET: /Claims/ClaimDetails/{id}
        public IActionResult ClaimDetails(int id)
        {
            try
            {
                var claim = new Claims();
                var details = new System.Collections.Generic.List<ClaimDetail>();

                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_GetClaimDetails", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClaimId", id);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            claim = new Claims
                            {
                                ClaimID = Convert.ToInt32(reader["ClaimId"]),
                                UserID = Convert.ToInt32(reader["UserId"]),
                                TotalHours = Convert.ToDecimal(reader["HoursWorked"]),
                                HourlyRate = Convert.ToDecimal(reader["HourlyRate"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"].ToString(),
                                SubmissionDate = Convert.ToDateTime(reader["DateSubmitted"]),
                                AdditionalNotes = reader["AdditionalNotes"].ToString()
                            };
                        }
                    }
                }

                ViewBag.Claim = claim;
                ViewBag.Details = details;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load claim details: " + ex.Message;
                return RedirectToAction("AdminApproval");
            }
        }

        // Add to ClaimsController.cs
        [HttpPost]
        public IActionResult BulkApprove(int[] claimIds)
        {
            try
            {
                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_BulkApproveClaims", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClaimIds", string.Join(",", claimIds));
                    cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("UserName"));

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Real-time notification
                TempData["Success"] = $"{claimIds.Length} claims approved successfully!";
                return Json(new { success = true, message = "Bulk approval completed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Add auto-email notifications
        private void SendApprovalNotification(int claimId, string lecturerEmail)
        {
            // Implement email service for automated notifications
        }
    }
}