using BookApp.Context;
using BookApp.Models.DTOs;
using BookApp.Models.Entities;
using BookApp.Models.ViewModels;
using BookApp.Repositories.Authors;
using BookApp.Repositories.Books;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;

        public BooksController(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetAllBooks()
        {
            var books = await _bookRepository.GetAllBooks();
            return Ok(books);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDTO>> GetBookById(int id)
        {
            var book = await _bookRepository.GetBookById(id);

            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(BookViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            int role = (int) HttpContext.Session.GetInt32("Role");
            if (role != (int) Role.Administrator)
            {
                return Unauthorized();
            }

            var book = new Book
            {
                Title = model.Title,
                Quantity = model.Quantity,
                AuthorId = model.AuthorId
            };

            await _bookRepository.PostBook(book);
            return Ok();
        }
    }
}
