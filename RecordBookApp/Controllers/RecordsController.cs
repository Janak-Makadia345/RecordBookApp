using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecordBookApp.Models;
using System.IO;

namespace RecordBookApp.Controllers
{
    public class RecordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Records
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Records
                .Include(r => r.Book)
                .Include(r => r.Category)
                .Include(r => r.Payment);

            var bookId = HttpContext.Session.GetString("BookId");
            if (bookId == null)
            {
                return RedirectToAction("Index", "Books");
            }

            int parsedBookId = int.Parse(bookId);

            // Fetch the BookName corresponding to the BookId
            var bookName = await _context.Books
                                         .Where(b => b.BookId == parsedBookId)
                                         .Select(b => b.BookName)
                                         .FirstOrDefaultAsync();

            var records = await applicationDbContext
                                      .Where(b => b.BookId == parsedBookId)
                                      .ToListAsync();

            // Calculate total cashin and cashout balances
            var totalCashIn = records.Where(r => r.Category.Type.ToLower() == "cashin").Sum(r => r.Amount);
            var totalCashOut = records.Where(r => r.Category.Type.ToLower() == "cashout").Sum(r => r.Amount);

            // Calculate net balance
            var netBalance = totalCashIn - totalCashOut;

            // Store the balances and BookName in ViewBag
            ViewBag.TotalCashIn = totalCashIn;
            ViewBag.TotalCashOut = totalCashOut;
            ViewBag.NetBalance = netBalance;
            ViewBag.BookName = bookName ?? "Book Not Found"; // Default value if bookName is null

            return View(records);
        }


        // GET: Records/Create
        public async Task<IActionResult> Create(string buttonClicked = null) // Optional buttonClicked parameter
        {
            RecordView recordView = new RecordView()
            {
                Date = DateTime.Now,
                Time = DateTime.Now
            };

            // Filter categories based on buttonClicked
            IEnumerable<Category> categories;
            if (buttonClicked == "cashin")
            {
                categories = _context.Categories.Where(c => c.Type.ToLower() == "cashin");
            }
            else if (buttonClicked == "cashout")
            {
                categories = _context.Categories.Where(c => c.Type.ToLower() == "cashout");
            }
            else
            {
                categories = _context.Categories; // Show all categories by default
            }

            ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "Name");
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Type");

            // Retrieve bookId from session
            var retrievedBookId = int.Parse(HttpContext.Session.GetString("BookId"));

            ViewData["BookId"] = retrievedBookId;

            return View(recordView);
        }

        // POST: Records/Create
        // POST: Records/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Date,Time,Amount,CategoryId,PaymentId,File")] RecordView recordView, IFormFile file)
        {
            var retrievedBookId = int.Parse(HttpContext.Session.GetString("BookId"));
            string fileName = null;

            if (file != null && file.Length > 0) // Check if a file was uploaded
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            var record = new Record
            {
                Date = recordView.Date,
                Time = recordView.Time,
                Amount = recordView.Amount,
                CategoryId = recordView.CategoryId,
                PaymentId = recordView.PaymentId,
                BookId = retrievedBookId,
                FileName = fileName // Save the file path or name in the database
            };

            _context.Add(record);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // GET: Records/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records.Include(r => r.Category).FirstOrDefaultAsync(r => r.RecordId == id);
            if (record == null)
            {
                return NotFound();
            }

            var recordView = new Record
            {
                RecordId = record.RecordId,
                Date = record.Date,
                Time = record.Time,
                Amount = record.Amount,
                CategoryId = record.CategoryId,
                PaymentId = record.PaymentId,
                FileName = record.FileName // Ensure FileName is populated for display
            };

            var categories = _context.Categories.Where(c => c.Type == record.Category.Type).ToList();
            ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "Name", record.CategoryId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Type", record.PaymentId);

            return View(recordView);
        }


        // POST: Records/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Record recordView, IFormFile file)
        {
            if (id != recordView.RecordId)
            {
                return NotFound();
            }

            var record = await _context.Records.Include(r => r.Category).FirstOrDefaultAsync(r => r.RecordId == id);
            if (record == null)
            {
                return NotFound();
            }

            var currentBookId = HttpContext.Session.GetString("BookId");

            if (record.BookId.ToString() != currentBookId)
            {
                return Forbid();
            }

            string fileName = record.FileName;

            if (file != null && file.Length > 0) // Check if a new file was uploaded
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            record.Date = recordView.Date;
            record.Time = recordView.Time;
            record.Amount = recordView.Amount;
            record.CategoryId = recordView.CategoryId;
            record.PaymentId = recordView.PaymentId;
            record.FileName = fileName; // Save the updated file path or name in the database

            try
            {
                _context.Update(record);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RecordExists(record.RecordId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Records/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records
                .Include(r => r.Book)
                .Include(r => r.Category)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(m => m.RecordId == id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        // POST: Records/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.Records.FindAsync(id);
            if (record != null)
            {
                _context.Records.Remove(record);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecordExists(int id)
        {
            return _context.Records.Any(e => e.RecordId == id);
        }

        // GET: Records/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var record = await _context.Records.FindAsync(id);
            if (record == null || string.IsNullOrEmpty(record.FileName))
            {
                return NotFound();
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            string filePath = Path.Combine(uploadsFolder, record.FileName);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", record.FileName);
        }

        // GET: Records/Preview/5
        public async Task<IActionResult> Preview(int id)
        {
            var record = await _context.Records.FindAsync(id);
            if (record == null || string.IsNullOrEmpty(record.FileName))
            {
                return NotFound();
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            string filePath = Path.Combine(uploadsFolder, record.FileName);

            string contentType;
            if (record.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/pdf";
            }
            else if (record.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     record.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/jpeg";
            }
            else if (record.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/png";
            }
            else
            {
                return Content("Preview is only available for PDF, JPG, JPEG, and PNG files.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType);
        }
    }
}
