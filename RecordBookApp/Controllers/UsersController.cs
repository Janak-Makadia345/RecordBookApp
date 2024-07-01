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
        public async Task<IActionResult> Create([Bind("UserId,Username,Email,Password,ConfirmPassword")] User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email address already in use.");
                return View(user);
            }

            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username already in use.");
                return View(user);
            }

            if(user.Password == null)
            {
                ModelState.AddModelError("Password", "Password field cannot be empty");
                return View(user);
            }

            if (user.ConfirmPassword == null)
            {
                ModelState.AddModelError("ConfirmPassword", "Confirm Password field cannot be empty");
                return View(user);
            }

            if (user.Password != user.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match. Please check & re-enter!");
                return View(user);
            }

            user.Password = HashPassword(user.Password);
            user.ConfirmPassword = HashPassword(user.ConfirmPassword);
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
                        Subject = "Verify Your Email Address",
                        Body = $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                            background-color: #ffffff;
                            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                        }}
                        .header {{
                            text-align: center;
                            padding: 10px 0;
                            border-bottom: 1px solid #e4e4e4;
                        }}
                        .header h1 {{
                            font-size: 24px;
                            margin: 0;
                        }}
                        .content {{
                            padding: 20px;
                            text-align: center;
                        }}
                        .content p {{
                            font-size: 16px;
                            margin: 0 0 20px;
                        }}
                        .content a {{
                            display: inline-block;
                            padding: 10px 20px;
                            font-size: 16px;
                            color: #ffffff;
                            background-color: #007bff;
                            text-decoration: none;
                            border-radius: 5px;
                        }}
                        .footer {{
                            text-align: center;
                            padding: 10px 0;
                            border-top: 1px solid #e4e4e4;
                            font-size: 14px;
                            color: #888888;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Record Book App Email Verification</h1>
                        </div>
                        <div class='content'>
                            <p>Thank you for registering! Please verify your email address by clicking the button below:</p>
                            <a href='{callbackUrl}'>Verify Email</a>
                        </div>
                        <div class='footer'>
                            <p>If you did not sign up for this account, you can ignore this email.</p>
                        </div>
                    </div>
                </body>
                </html>",
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

            if (user == null)
            {
                return NotFound();
            }

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
        public async Task<IActionResult> SignIn([Bind("Username,Email,Password")] User model)
        {
            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Username))
            {
                ModelState.AddModelError("Error", "Please enter either an email address or a username.");
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email || u.Username == model.Username);

            if (user == null)
            {
                ModelState.AddModelError("Error", "User not found. Please sign up.");
                return View(model);
            }

            if (user.IsEmailVerified == false)
            {
                ModelState.AddModelError("Error", "User not verified. Please verify by clicking the link sent to your email.");
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
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Username,Email,Password")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            
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
