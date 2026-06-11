using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GK_Web.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên học phần là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên học phần không quá 200 ký tự")]
        [Display(Name = "Tên học phần")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Số tín chỉ là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Số tín chỉ phải từ 1 đến 10")]
        [Display(Name = "Số tín chỉ")]
        public int Credits { get; set; }

        [Required(ErrorMessage = "Tên giảng viên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên giảng viên không quá 100 ký tự")]
        [Display(Name = "Giảng viên")]
        public string Lecturer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<Enrollment>? Enrollments { get; set; }

        // Helper to get meaningful abbreviation for course
        public string GetAbbreviation()
        {
            if (string.IsNullOrEmpty(Name)) return "HP";

            var nameLower = Name.ToLower().Trim();
            
            // Custom mappings for standard courses
            if (nameLower.Contains("cấu trúc dữ liệu")) return "CTDL";
            if (nameLower.Contains("cơ sở dữ liệu")) return "CSDL";
            if (nameLower.Contains("asp.net")) return "ASP.NET";
            if (nameLower.Contains("hướng đối tượng") || nameLower.Contains("oop")) return "OOP";
            if (nameLower.Contains("phân tích") && nameLower.Contains("thiết kế")) return "PTTK";
            if (nameLower.Contains("toán cao cấp")) return "TOÁN";
            if (nameLower.Contains("tiếng anh") || nameLower.Contains("english")) return "ENG";
            if (nameLower.Contains("quản trị kinh doanh")) return "QTKD";
            if (nameLower.Contains("c#")) return "C#";

            // Fallback: extract initials of uppercase words
            var words = Name.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var initials = "";
            foreach (var word in words)
            {
                if (word.Length > 0 && char.IsUpper(word[0]))
                {
                    initials += word[0];
                }
            }

            if (initials.Length > 1) return initials;

            // Otherwise first 2 letters
            return Name.Length > 1 ? Name.Substring(0, 2).ToUpper() : Name.ToUpper();
        }
    }
}
