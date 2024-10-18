using BookApp.Context;
using BookApp.Models.DTOs;
using BookApp.Models.Entities;
using BookApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Net.NetworkInformation;

namespace BookApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoansController : Controller
    {
        private readonly AppDbContext _context;

        public LoansController(AppDbContext context)
        {
            _context = context;
        }

        // Pobranie trwających wypożyczeń przez pracownika/administratora
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanDTO>>> GetAllLoans()
        {
            if(HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            int userId = (int)HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if(user.Role != Role.Administrator && user.Role != Role.Pracownik)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var allLoans = await _context.Loans.Select(l => new LoanDTO
            {
                LoanId = l.LoanId,
                User = new LoanUserDTO
                {
                    UserId = l.User.UserId,
                    Login = l.User.Login,
                    Email = l.User.Email,
                    FirstName = l.User.FirstName,
                    LastName = l.User.LastName,
                    Street = l.User.Street,
                    City = l.User.City,
                    Role = l.User.Role.ToString(),
                    SignupDate = l.User.SignupDate
                },
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                Status = l.Status.ToString(),
                Books = l.LoanItems.Select(li => new LoanBookDTO
                {
                    BookId = li.BookId,
                    Title = li.Book.Title,
                    Author = new AuthorDTO
                    {
                        AuthorId = li.Book.AuthorId,
                        FirstName = li.Book.Author.FirstName,
                        LastName = li.Book.Author.LastName
                    }
                }).ToList()
            }).ToListAsync();

            var ongoingLoans = allLoans.Where(l => l.Status == LoanStatus.Trwające.ToString()).ToList();

            return Ok(ongoingLoans);
        }

        // pobranie rezerwacji przez pracownika/administratora
        [HttpGet("Reservations")]
        public async Task<ActionResult<IEnumerable<LoanDTO>>> GetAllReservations()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            int userId = (int)HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user.Role != Role.Administrator && user.Role != Role.Pracownik)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var allLoans = await _context.Loans.Select(l => new LoanDTO
            {
                LoanId = l.LoanId,
                User = new LoanUserDTO
                {
                    UserId = l.User.UserId,
                    Login = l.User.Login,
                    Email = l.User.Email,
                    FirstName = l.User.FirstName,
                    LastName = l.User.LastName,
                    Street = l.User.Street,
                    City = l.User.City,
                    Role = l.User.Role.ToString(),
                    SignupDate = l.User.SignupDate
                },
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                Status = l.Status.ToString(),
                Books = l.LoanItems.Select(li => new LoanBookDTO
                {
                    BookId = li.BookId,
                    Title = li.Book.Title,
                    Author = new AuthorDTO
                    {
                        AuthorId = li.Book.AuthorId,
                        FirstName = li.Book.Author.FirstName,
                        LastName = li.Book.Author.LastName
                    }
                }).ToList()
            }).ToListAsync();

            var reservations = allLoans.Where(l => l.Status == LoanStatus.Zarezerwowane.ToString()).ToList();

            return Ok(reservations);
        }

        // dodanie nowej rezerwacji
        [HttpPost]
        public async Task<ActionResult<LoanResponseDTO>> AddReservation(LoanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return Unauthorized(new
                    {
                        message = "Nie jesteś zalogowany."
                    });
                }

                var userId = (int) HttpContext.Session.GetInt32("UserId");
                var books = await _context.Books
                    .Include(b => b.Author)
                    .Where(b => model.BookIds.Contains(b.BookId))
                    .ToListAsync();

                if (books.Count != model.BookIds.Count)
                {
                    var existingBookIds = books.Select(b => b.BookId);
                    var missingBookIds = model.BookIds.Except(existingBookIds);
                    return NotFound(new
                    {
                        message = $"Książki o ID {string.Join(", ", missingBookIds)} nie zostały znalezione."
                    });
                }

                var activeStatuses = new[] { LoanStatus.Zarezerwowane, LoanStatus.Trwające };
                var unavailableBooks = books.Where(b =>
                {
                    var activeLoansCount = _context.LoanItems
                        .Include(li => li.Loan)
                        .Where(li => li.BookId == b.BookId && activeStatuses.Contains(li.Loan.Status))
                        .Count();
                    return (b.Quantity - activeLoansCount) < model.BookIds.Count(id => id == b.BookId);
                }).ToList();

                if (unavailableBooks.Any())
                {
                    var messages = unavailableBooks.Select(b => $"Książka '{b.Title}' jest niedostępna");
                    return BadRequest(new { message = messages });
                }

                var loan = new Loan
                {
                    UserId = userId,
                    LoanItems = new List<LoanItem>()
                };

                foreach (var bookId in model.BookIds)
                {
                    loan.LoanItems.Add(new LoanItem
                    {
                        BookId = bookId,
                    });
                }

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var loanResponse = new LoanResponseDTO
                {
                    LoanId = loan.LoanId,
                    DateFrom = loan.DateFrom,
                    DateTo = loan.DateTo,
                    Status = loan.Status.ToString(),
                    Books = loan.LoanItems
                                .Select(li => new LoanBookDTO
                                {
                                    BookId = li.BookId,
                                    Title = books.First(b => b.BookId == li.BookId).Title,
                                    Author = new AuthorDTO
                                    {
                                        AuthorId = books.First(b => b.BookId == li.BookId).Author.AuthorId,
                                        FirstName = books.First(b => b.BookId == li.BookId).Author.FirstName,
                                        LastName = books.First(b => b.BookId == li.BookId).Author.LastName
                                    }
                                })
                                .ToList()
                };

                return Ok(loanResponse);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Wystąpił błąd podczas tworzenia wypożyczenia.",
                    details = ex.Message
                });
            }
        }

        // wydłużenie wypożyczenia o miesiąc przez wypożyczającego
        [HttpPut("{id}/Extend")]
        public async Task<ActionResult<Loan>> ExtendLoan(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var userId = (int) HttpContext.Session.GetInt32("UserId");
            var loan = await _context.Loans
                .Include(l => l.LoanItems)
                    .ThenInclude(li => li.Book)
                .FirstOrDefaultAsync(l => l.LoanId == id);

            if(loan == null)
            {
                return NotFound(new { message = $"Wypożyczenie o ID {id} nie zostało znalezione." });
            }

            if(loan.UserId != userId)
            {
                return Unauthorized();
            }

            if(loan.Status != LoanStatus.Trwające && loan.Status != LoanStatus.Zarezerwowane)
            {
                return BadRequest(new {message = "Tylko aktywne wypożyczenia mogą zostać wydłużone."});
            }

            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            if(currentDate > loan.DateTo)
            {
                return BadRequest(new { message = "Nie można wydłużyć wypożyczenia, ponieważ termin zwrotu już minął" });
            }

            var totalDuration = (loan.DateTo.ToDateTime(TimeOnly.MinValue) - loan.DateFrom.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (totalDuration >= 93)
            {
                return BadRequest(new { message = "Wypożyczenie nie może trwać dłużej niż 3 miesiące." });
            }

            var activeStatuses = new[] { LoanStatus.Zarezerwowane, LoanStatus.Trwające };
            foreach(var loanItem in loan.LoanItems)
            {
                var book = loanItem.Book;
                var activeLoanCount = await _context.LoanItems
                    .Where(li => li.BookId == book.BookId && activeStatuses.Contains(li.Loan.Status))
                    .CountAsync();
                int availableCopies = book.Quantity - activeLoanCount;

                if(availableCopies < 1)
                {
                    return BadRequest(new { message = $"Książka '{book.Title}' nie ma dostępnych egzemplarzy, by móc wydłużyć wypożyczenie." });
                }
            }

            loan.DateTo = loan.DateTo.AddMonths(1);
            var newDuration = (loan.DateTo.ToDateTime(TimeOnly.MinValue) - loan.DateFrom.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if(newDuration > 93)
            {
                return BadRequest(new { message = "Wypożyczenie nie może trwać dłużej niż 3 miesiące." });
            }

            try
            {
                _context.Loans.Update(loan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd", details = ex.Message });
            }

            return Ok(new { message = $"Wypożyczenie zostało wydłużone do '{loan.DateTo}'" });
        }

        // anulowanie wszystkich przestarzałych rezerwacji przez pracownika/administratora
        [HttpPut("CancelExpiredReservations")]
        public async Task<ActionResult<Loan>> CancelExpiredReservations()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized();
            }

            var userId = (int)HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user.Role != Role.Pracownik && user.Role != Role.Administrator)
            {
                return Unauthorized();
            }

            var reservationValidityPeriod = TimeSpan.FromDays(2);
            var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));

            var allReservations = await _context.Loans
                .Where(l => l.Status == LoanStatus.Zarezerwowane)
                .ToListAsync();

            var expiredReservations = allReservations
                .Where(l => l.DateFrom <= cutoffDate)
                .ToList();

            if (expiredReservations == null || !expiredReservations.Any())
            {
                return Ok(new { message = "Brak przestarzałych rezerwacji." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var loan in expiredReservations)
                {
                    loan.Status = LoanStatus.Anulowane;
                }

                _context.Loans.UpdateRange(expiredReservations);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"{expiredReservations.Count} rezerwacji zostało anulowanych." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Wystąpił błąd podczas anulowania przestarzałych rezerwacji.",
                    details = ex.Message
                });
            }
        }

        // anulowanie rezerwacji przez rezerwującego
        [HttpPut("{id}/CancelReservation")]
        public async Task<ActionResult<Loan>> CancelReservation(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var userId = (int) HttpContext.Session.GetInt32("UserId");
            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanId == id);

            if(loan == null)
            {
                return NotFound(new { message = $"Wypożyczenie o ID {id} nie zostało znalezione." });
            }

            if(loan.UserId != userId)
            {
                return Unauthorized();
            }

            if(loan.Status != LoanStatus.Zarezerwowane)
            {
                return BadRequest(new {message = "Tylko rezerwacje mogą być anulowane."});
            }

            loan.Status = LoanStatus.Anulowane;
            try
            {
                _context.Loans.Update(loan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd", details = ex.Message });
            }

            return Ok(new { message = "Rezerwacja została anulowana." });
        }

        // rozpoczęcie wypożyczenia przez pracownika/administratora
        [HttpPut("{id}/Start")]
        public async Task<ActionResult<Loan>> StartLoan(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var userId = (int)HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user.Role != Role.Pracownik && user.Role != Role.Administrator)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanId == id);
            if (loan == null)
            {
                return NotFound(new { message = $"Wypożyczenie o ID {id} nie zostało znalezione." });
            }

            if (loan.Status != LoanStatus.Zarezerwowane)
            {
                return BadRequest(new
                {
                    message = "Oznaczyć jako trwające wypożyczenie można jedynie rezerwację."
                });
            }

            loan.Status = LoanStatus.Trwające;
            try
            {
                _context.Loans.Update(loan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd", details = ex.Message });
            }

            return Ok(new { message = "Wypożyczenie zostało rozpoczęte." });
        }

        // zakończenie wypożyczenia przez pracownika/administratora
        [HttpPut("{id}/Finish")]
        public async Task<ActionResult<Loan>> FinishLoan(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var userId = (int)HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user.Role != Role.Pracownik && user.Role != Role.Administrator)
            {
                return Unauthorized(new
                {
                    message = "Brak uprawnień."
                });
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanId == id);
            if (loan == null)
            {
                return NotFound(new { message = $"Wypożyczenie o ID {id} nie zostało znalezione." });
            }

            if (loan.Status != LoanStatus.Trwające)
            {
                return BadRequest(new
                {
                    message = "Zakończyć można jedynie trwające wypożyczenie."
                });
            }

            loan.Status = LoanStatus.Zakończone;
            try
            {
                _context.Loans.Update(loan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd", details = ex.Message });
            }

            return Ok(new { message = "Wypożyczenie zostało zakończone." });
        }
    }
}
