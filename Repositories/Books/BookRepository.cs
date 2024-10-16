using BookApp.Context;
using BookApp.Models.DTOs;
using BookApp.Models.Entities;
using BookApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookApp.Repositories.Books
{
    public class BookRepository : IBookRepository
    {
        private readonly AppDbContext _context;
        
        public BookRepository(AppDbContext context) 
        {
            _context = context;
        }
        
        public async Task<IEnumerable<BookDTO>> GetAllBooks()
        {
            var books = await _context.Books
                                      .Include(b => b.Author)
                                      .Select(b => new BookDTO
                                      {
                                          BookId = b.BookId,
                                          Title = b.Title,
                                          Author = new AuthorDTO
                                          {
                                              AuthorId = b.Author.AuthorId,
                                              FirstName = b.Author.FirstName,
                                              LastName = b.Author.LastName
                                          },
                                          AvailableCopies = b.Quantity - _context.LoanItems
                                                .Where
                                                (
                                                    li => li.BookId == b.BookId &&
                                                    (li.Loan.Status == LoanStatus.Trwające || li.Loan.Status == LoanStatus.Zarezerwowane)
                                                )
                                                .Count()
                                      })
                                      .ToListAsync();

            return books;
        }

        public async Task<BookDTO> GetBookById(int id)
        {
            var book = await _context.Books
                                      .Include(b => b.Author)
                                      .Select(b => new BookDTO
                                      {
                                          BookId = b.BookId,
                                          Title = b.Title,
                                          Author = new AuthorDTO
                                          {
                                              AuthorId = b.Author.AuthorId,
                                              FirstName = b.Author.FirstName,
                                              LastName = b.Author.LastName
                                          },
                                          AvailableCopies = b.Quantity - _context.LoanItems
                                                .Where
                                                (
                                                    li => li.BookId == b.BookId &&
                                                    (li.Loan.Status == LoanStatus.Trwające || li.Loan.Status == LoanStatus.Zarezerwowane)
                                                )
                                                .Count()
                                      })
                                      .FirstOrDefaultAsync(b => b.BookId == id);

            return book;
        }

        public async Task PostBook(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }
    }
}
