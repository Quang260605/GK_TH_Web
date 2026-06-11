using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace GK_Web.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Sinh viên")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Học phần")]
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "Ngày đăng ký")]
        public DateTime EnrollDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }
    }
}
