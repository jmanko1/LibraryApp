using BookApp.Models.DTOs;
using BookApp.Models.Entities;

namespace BookApp.Repositories.Books
{
    public interface IBookRepository
    {
        Task<IEnumerable<BookDTO>> GetAllBooks();
        Task<BookDTO> GetBookById(int id);
        Task PostBook(Book book);
    }
}
