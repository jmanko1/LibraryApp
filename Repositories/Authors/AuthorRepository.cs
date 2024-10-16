using BookApp.Context;
using BookApp.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookApp.Repositories.Authors
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly AppDbContext _context;

        public AuthorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAuthor(Author author)
        {
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Author>> GetAllAuthors()
        {
            var authors = await _context.Authors
                                    .Include(a => a.Books)
                                    .ToListAsync();
            
            return authors;
        }

        public async Task<Author> GetAuthorById(int id)
        {
            var author = await _context.Authors
                                       .Include(a => a.Books)
                                       .FirstOrDefaultAsync(a => a.AuthorId == id);

            return author;
        }
    }
}
