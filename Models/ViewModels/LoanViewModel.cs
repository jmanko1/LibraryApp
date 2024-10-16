using System.ComponentModel.DataAnnotations;

namespace BookApp.Models.ViewModels
{
    public class LoanViewModel
    {
        [MinLength(1, ErrorMessage = "Musisz wybrać przynajmniej jedną książkę")]
        public ICollection<int> BookIds { get; set; }
    }
}
