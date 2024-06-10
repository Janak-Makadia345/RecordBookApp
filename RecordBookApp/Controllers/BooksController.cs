using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RecordBookApp.Models;

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
            return RedirectToAction("Index","Records");
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                // Handle case where user is not logged in
                return RedirectToAction("SignIn", "Users");
            }

            int parsedUserId = int.Parse(userId);
            var books = await _context.Books
                                      .Where(b => b.UserId == parsedUserId)
                                      .ToListAsync();
            return View(books);
        }


        // GET: Books/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookView bookView)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return View("Error"); // Handle the case where UserId is not found in the session
            }

            // Check if user exists before saving
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                // Handle user not found scenario (e.g., display error message)
                return View("Error"); // Or redirect to appropriate error page
            }

            var book = new Book
            {
                BookName = bookView.BookName,
                UserId = int.Parse(userId)
            };

            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Log validation errors
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Debug.WriteLine($"Error: {error.ErrorMessage}"); // Log the error message
                if (error.Exception != null)
                {
                    Debug.WriteLine($"Exception: {error.Exception.Message}"); // Log the exception message if available
                }
            }

            return View(bookView);
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

            return View(viewModel); // Pass viewModel to the view
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

                    // Create a new Book object
                    var updatedBook = new Book
                    {
                        BookId = id,
                        BookName = inputModel.BookName,
                        UserId = book.UserId // Retain the original UserId
                    };

                    // Update the database with the new Book object
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

            // If ModelState is not valid, return to the view with the inputModel
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

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
