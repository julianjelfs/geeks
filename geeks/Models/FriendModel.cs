using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace geeks.Models
{
    public class UserModel
    {
        public string UserName { get; set; }
        public List<FriendModel> Friends { get; set; }
    }

    public class FriendModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string Name { get; set; }
        
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}