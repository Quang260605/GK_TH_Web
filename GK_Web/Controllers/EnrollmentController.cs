using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GK_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GK_Web.Controllers
{
    [Authorize(Roles = "Student")]
    [Route("enroll")]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EnrollmentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /enroll/mycourses (Question 7 requirement)
        [HttpGet("mycourses")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c!.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EnrollDate)
                .ToListAsync();

            return View(enrollments);
        }

        // POST: /enroll/enroll (Question 6 requirement)
        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập." });
            }

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
            if (!courseExists)
            {
                return NotFound(new { success = false, message = "Học phần không tồn tại." });
            }

            // Check if already enrolled
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == request.CourseId && e.UserId == userId);

            if (alreadyEnrolled)
            {
                return BadRequest(new { success = false, message = "Bạn đã đăng ký học phần này." });
            }

            var enrollment = new Enrollment
                {
                    UserId = userId,
                    CourseId = request.CourseId,
                    EnrollDate = DateTime.Now
                };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đăng ký học phần thành công." });
        }

        // POST: /enroll/cancel (Question 6 requirement)
        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] EnrollRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập." });
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.UserId == userId);

            if (enrollment == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin đăng ký học phần." });
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã hủy đăng ký học phần thành công." });
        }
    }
}
