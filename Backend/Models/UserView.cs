using Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace Backend.Models
{
    [NotMapped]
    public class UserView : User
    {
        [Display(Name = "Picture")]
        public HttpPostedFileBase PictureFile { get; set; }

        [Display(Name = "Favorite league")]
        public int FavoriteLeagueId { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(20, ErrorMessage = "The length for the field {0} must be between {1} and {2} characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "The field {0} is required")]
        [Compare("Password", ErrorMessage = "The password and confirm does not match")]
        [Display(Name = "Password confirm")]
        public string PasswordConfirm { get; set; }
    }
}