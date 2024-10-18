using BookApp.Context;
using BookApp.Models.DTOs;
using BookApp.Models.Entities;
using BookApp.Models.ViewModels;
using BookApp.Repositories.Authors;
using BookApp.Repositories.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IUserRepository _userRepository;

        public AuthorsController(IAuthorRepository authorRepository, IUserRepository userRepository)
        {
            _authorRepository = authorRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Author>>> GetAuthors()
        {
            var authors = await _authorRepository.GetAllAuthors();

            return Ok(authors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Author>> GetAuthorById(int id)
        {
            var author = await _authorRepository.GetAuthorById(id);
            if (author == null)
            {
                return NotFound();
            }

            return Ok(author);
        }

        [HttpPost]
        public async Task<IActionResult> PostAuthor(AuthorViewModel model)
        {
            if(HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            int userId = (int) HttpContext.Session.GetInt32("UserId");
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user.Role != Role.Administrator)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var author = new Author
            {
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            await _authorRepository.AddAuthor(author);
            return Ok(author);
        }
    }
}
