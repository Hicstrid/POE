using Microsoft.AspNetCore.Mvc;
using POE.Models;
using POE.Services;
using System.Data.SqlClient;
using System.Diagnostics;

namespace POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DatabaseService _db;

        // FIXED: Single constructor with both dependencies
        public HomeController(ILogger<HomeController> logger, DatabaseService db)
        {
            _logger = logger;
            _db = db;
        }

        // GET: /Home/Index (Login Page)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/Registration (Registration Page)
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(User model)
        {
            try
            {
                Console.WriteLine($"Login attempt for user: {model.username}");

                using (var con = _db.GetConnection())
                using (var cmd = new SqlCommand("SP_UserLogin", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Email", model.email);
                    cmd.Parameters.AddWithValue("@PasswordHash", model.password);

                    con.Open();
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        int userId = (int)reader["UserId"];
                        string role = reader["Role"].ToString();

                        // Save user info in session
                        HttpContext.Session.SetInt32("UserId", userId);
                        HttpContext.Session.SetString("UserRole", role);
                        HttpContext.Session.SetString("UserName", reader["FirstName"].ToString());

                        Console.WriteLine($"Login SUCCESS - UserId: {userId}, Role: {role}");
                        Console.WriteLine($"Redirecting to Claims/SubmitClaim...");

                        return RedirectToAction("TestRedirect", "Home");
                    }
                }

                ViewData["LoginError"] = "Invalid credentials";
                return View("Index", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login ERROR: {ex.Message}");
                ViewData["LoginError"] = "Login failed: " + ex.Message;
                return View("Index", model);
            }
        }

        [HttpPost]
        public IActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var con = _db.GetConnection())
                    {
                        using (var cmd = new SqlCommand("SP_RegisterUser", con))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@FirstName", model.Name);
                            cmd.Parameters.AddWithValue("@LastName", model.Surname);
                            cmd.Parameters.AddWithValue("@Email", model.email);
                            cmd.Parameters.AddWithValue("@PasswordHash", model.password);
                            cmd.Parameters.AddWithValue("@Username", model.username);
                            cmd.Parameters.AddWithValue("@Role", model.role);

                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }

                    return RedirectToAction("Index"); // Redirect to login page after registration
                }
                catch (Exception ex)
                {
                    ViewData["Error"] = "Registration failed: " + ex.Message;
                    return View("Registration", model);
                }
            }

            // If model validation fails
            return View("Registration", model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult TestRedirect()
        {
            Console.WriteLine("TestRedirect reached successfully!");
            return Content("<h1>Redirect Working!</h1><p>Claims section is reachable.</p>");
        }
    }
}