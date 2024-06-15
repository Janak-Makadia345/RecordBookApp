using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecordBookApp.Models;

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
            var records = await applicationDbContext
                                      .Where(b => b.BookId == parsedBookId)
                                      .ToListAsync();

            // Calculate total cashin and cashout balances
            var totalCashIn = records.Where(r => r.Category.Type.ToLower() == "cashin").Sum(r => r.Amount);
            var totalCashOut = records.Where(r => r.Category.Type.ToLower() == "cashout").Sum(r => r.Amount);

            // Calculate net balance
            var netBalance = totalCashIn - totalCashOut;

            // Store the balances in ViewBag
            ViewBag.TotalCashIn = totalCashIn;
            ViewBag.TotalCashOut = totalCashOut;
            ViewBag.NetBalance = netBalance;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Date,Time,Amount,CategoryId,PaymentId")] RecordView recordView)
        {
            var retrievedBookId = int.Parse(HttpContext.Session.GetString("BookId"));

            var record = new Record
            {
                Date = recordView.Date,
                Time = recordView.Time,
                Amount = recordView.Amount,
                CategoryId = recordView.CategoryId,
                PaymentId = recordView.PaymentId,
                BookId = retrievedBookId
            };

            if (ModelState.IsValid)
            {
                _context.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Type", recordView.CategoryId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Type", recordView.PaymentId);
            return View(recordView);
        }


        // GET: Records/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", record.CategoryId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Type", record.PaymentId);
            return View(record);
        }

        // POST: Records/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecordView recordView)
        {
            if (id == null)
            {
                return NotFound();
            }

            var record = await _context.Records.FindAsync(id);

            var currentBookId = HttpContext.Session.GetString("BookId");

            if (record == null || record.BookId.ToString() != currentBookId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                 try
                 {
                    _context.Entry(record).State = EntityState.Detached;

                    // Update the actual Record model
                    var updatedRecord = new Record
                     {
                         RecordId = id,
                         Date = recordView.Date,
                         Time = recordView.Time,
                         Amount = recordView.Amount,
                         PaymentId = recordView.PaymentId,
                         CategoryId = recordView.CategoryId,
                         BookId = record.BookId // Retain the original UserId
                     };

                     // Update other properties if needed
                     _context.Update(updatedRecord);
                     await _context.SaveChangesAsync();
                 }
                 catch (DbUpdateConcurrencyException)
                 {
                     if (!RecordExists(record.BookId))
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

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", recordView.CategoryId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Type", recordView.PaymentId);
            return View(recordView);

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
        }
    }

