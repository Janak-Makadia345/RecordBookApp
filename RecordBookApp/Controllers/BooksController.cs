using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Books/Create
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var book = new Book();

            if (userId != null)
            {
                book.UserId = int.Parse(userId); // Parse the string UserId back to int
                ViewData["UserId"] = userId;
            }
            return View(book);
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,BookName,UserId")] Book book)
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Check if user exists before saving
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                // Handle user not found scenario (e.g., display error message)
                return View("Error"); // Or redirect to appropriate error page
            }

            book.UserId = userId;

            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
            
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (book.UserId.ToString() != currentUserId)
            {
                return Forbid();
            }

            ViewData["UserId"] = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = book.UserId.ToString(), Text = _context.Users.Find(book.UserId).Email }
            }, "Value", "Text");

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookId,BookName,UserId")] Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (book.UserId.ToString() != currentUserId)
            {
                return Forbid();
            }

                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
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

            ViewData["UserId"] = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = book.UserId.ToString(), Text = _context.Users.Find(book.UserId).Email }
            }, "Value", "Text");

            return View(book);
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
