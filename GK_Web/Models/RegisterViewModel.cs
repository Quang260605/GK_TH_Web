using System.ComponentModel.DataAnnotations;

namespace GK_Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đăng nhập tối thiểu {2} và tối đa {1} ký tự", MinimumLength = 3)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu tối thiểu phải {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
