using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RecordBookApp.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Diagnostics;

namespace RecordBookApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Email,Password")] User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email address already in use.");
                return View(user);
            }

            user.Password = HashPassword(user.Password);
            user.EmailVerificationToken = GenerateEmailVerificationToken();
            user.IsEmailVerified = false;

            _context.Add(user);
            await _context.SaveChangesAsync();

            SendVerificationEmail(user.Email, user.EmailVerificationToken);

            // Inform user to check their email
            ViewBag.Message = "Registration successful! Please check your email to verify your account.";
            return View("Create");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GenerateEmailVerificationToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        private void SendVerificationEmail(string email, string token)
        {
            var callbackUrl = Url.Action("VerifyEmail", "Users", new { token }, protocol: Request.Scheme);

            var smtpSettings = _configuration.GetSection("Smtp").Get<SmtpSettings>();

            try
            {
                using (var client = new SmtpClient(smtpSettings.Host, smtpSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpSettings.Username),
                        Subject = "Verify your email",
                        Body = $"Please verify your email by clicking <a href='{callbackUrl}'>here</a>.",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    client.Send(mailMessage);
                }
            }
            catch (SmtpException ex)
            {
                // Write exception to debug output
                Debug.WriteLine($"Failed to send verification email to {email}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Write exception to debug output
                Debug.WriteLine($"An error occurred while sending verification email to {email}. Error: {ex.Message}");
            }
        }


        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmailVerificationToken == token);

            user.IsEmailVerified = true;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("SignIn", "Users");
        }

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn([Bind("Email,Password")] User model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Error", "User not found. Please SignUp.");
                return View(model);
            }

            if(user.IsEmailVerified == false)
            {
                ModelState.AddModelError("Error", "User not verified. Please verify by clicking the link sent to Email.");
                return View(model);
            }

            if (VerifyPassword(model.Password, user.Password))
            {
                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                return RedirectToAction("Index", "Books");
            }
            else
            {
                ModelState.AddModelError("Password", "Invalid password. Please try again.");
                return View(model);
            }
        }

        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                var enteredHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                var enteredHashString = Convert.ToBase64String(enteredHash);
                return enteredHashString == storedPassword;
            }
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Email,Password")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.Password = HashPassword(user.Password); // Re-hash the password before saving
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
