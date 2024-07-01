using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecordBookApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace RecordBookApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> BookIdretrieval(int bookId)
        {
            HttpContext.Session.SetString("BookId", bookId.ToString());
            return RedirectToAction("Index", "Records");
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                return RedirectToAction("SignIn", "Users");
            }

            int parsedUserId = int.Parse(userId);
            var booksQuery = _context.Books.Where(b => b.UserId == parsedUserId);

            var books = await booksQuery.ToListAsync();
            var bookViewModels = new List<BookView>();

            foreach (var book in books)
            {
                var cashInTotal = _context.Records
                    .Where(r => r.BookId == book.BookId && r.Category.Type == "CashIn")
                    .Sum(r => r.Amount);

                var cashOutTotal = _context.Records
                    .Where(r => r.BookId == book.BookId && r.Category.Type == "CashOut")
                    .Sum(r => r.Amount);

                var netBalance = cashInTotal - cashOutTotal;

                // Get the latest record update time
                var lastUpdatedRecord = _context.Records
                    .Where(r => r.BookId == book.BookId)
                    .OrderByDescending(r => r.Date).ThenByDescending(r => r.Time)
                    .FirstOrDefault();

                var updatedAt = lastUpdatedRecord != null ? (DateTime?)new DateTime(lastUpdatedRecord.Date.Year, lastUpdatedRecord.Date.Month, lastUpdatedRecord.Date.Day, lastUpdatedRecord.Time.Hour, lastUpdatedRecord.Time.Minute, lastUpdatedRecord.Time.Second) : null;

                bookViewModels.Add(new BookView
                {
                    BookId = book.BookId,
                    BookName = book.BookName,
                    NetBalance = netBalance,
                    CreatedAt = book.CreatedAt,
                    UpdatedAt = updatedAt
                });
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // If it's an AJAX request, return only the list of books as JSON
                return Json(bookViewModels);
            }

            // Otherwise, render the full Index view with the list of books
            return View(bookViewModels);
        }



        // GET: Books/Create
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_CreatePartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookView bookView)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User ID not found in session" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var book = new Book
            {
                BookName = bookView.BookName,
                UserId = int.Parse(userId),
                CreatedAt = DateTime.Now
            };

            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return PartialView("_CreatePartial", bookView);
        }


        // GET: Books/Edit/5
        public IActionResult Edit(int id)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            var book = _context.Books.Find(id);
            if (book == null || book.UserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            var viewModel = new BookView
            {
                BookName = book.BookName
            };

            return View(viewModel);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookView inputModel)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            var book = await _context.Books.FindAsync(id);
            if (book == null || book.UserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(book).State = EntityState.Detached;

                    var updatedBook = new Book
                    {
                        BookId = id,
                        BookName = inputModel.BookName,
                        UserId = book.UserId,
                        CreatedAt = book.CreatedAt // Preserve the original CreatedAt value
                    };

                    _context.Update(updatedBook);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(inputModel);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return PartialView("_DeletePartial", book);
        }

        // POST: Books/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int BookId)
        {
            var book = await _context.Books.FindAsync(BookId);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, errorMessage = "Failed to delete book." });
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
