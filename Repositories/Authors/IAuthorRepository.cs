using BookApp.Models.Entities;

namespace BookApp.Repositories.Authors
{
    public interface IAuthorRepository
    {
        Task<IEnumerable<Author>> GetAllAuthors();
        Task<Author> GetAuthorById(int id);
        Task AddAuthor(Author author);
    }
}
