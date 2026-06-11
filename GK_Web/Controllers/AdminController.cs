using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GK_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GK_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /admin/dashboard (Question 10)
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            // 1. Tổng số học phần
            var totalCourses = await _context.Courses.CountAsync();

            // 2. Tổng số sinh viên (Đếm số người dùng thuộc vai trò Student)
            var studentRole = await _roleManager.FindByNameAsync("Student");
            var totalStudents = studentRole != null 
                ? await _context.UserRoles.CountAsync(ur => ur.RoleId == studentRole.Id) 
                : 0;

            // 3. Tổng số lượt đăng ký
            var totalEnrollments = await _context.Enrollments.CountAsync();

            // Lấy danh sách 5 lượt đăng ký gần nhất để hiển thị ở bảng
            var recentEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .OrderByDescending(e => e.EnrollDate)
                .Take(5)
                .ToListAsync();

            // Tổng hợp thống kê số lượng đăng ký cho từng học phần phục vụ vẽ biểu đồ
            var courseStats = await _context.Courses
                .Select(c => new
                {
                    CourseName = c.Name,
                    Count = c.Enrollments != null ? c.Enrollments.Count : 0
                })
                .ToListAsync();

            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalEnrollments = totalEnrollments;
            ViewBag.RecentEnrollments = recentEnrollments;
            
            // Dữ liệu biểu đồ Chart.js
            ViewBag.ChartLabels = courseStats.Select(s => s.CourseName).ToArray();
            ViewBag.ChartData = courseStats.Select(s => s.Count).ToArray();

            return View();
        }

        // GET: /admin/courses (Danh sách quản lý học phần)
        [HttpGet("")]
        [HttpGet("courses")]
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Category)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(courses);
        }

        // POST: /admin/categories/create
        [HttpPost("categories/create")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, message = "Tên danh mục không được để trống." });
            }

            var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower());
            if (exists)
            {
                return BadRequest(new { success = false, message = "Tên danh mục đã tồn tại." });
            }

            var category = new Category { Name = request.Name.Trim() };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = category.Id, name = category.Name });
        }

        // GET: /admin/courses/create (Question 2)
        [HttpGet("courses/create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: /admin/courses/create (Question 2)
        [HttpPost("courses/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Credits,Lecturer,CategoryId")] Course course, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    course.Image = await SaveImageAsync(imageFile);
                }

                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", course.CategoryId);
            return View(course);
        }

        // GET: /admin/courses/edit/{id} (Question 2)
        [HttpGet("courses/edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", course.CategoryId);
            return View(course);
        }

        // POST: /admin/courses/edit/{id} (Question 2)
        [HttpPost("courses/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Image,Credits,Lecturer,CategoryId")] Course course, IFormFile? imageFile)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(course.Image))
                        {
                            DeleteOldImage(course.Image);
                        }
                        course.Image = await SaveImageAsync(imageFile);
                    }

                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", course.CategoryId);
            return View(course);
        }

        // POST: /admin/courses/delete/{id} (Question 2)
        [HttpPost("courses/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            // Delete image file if exists
            if (!string.IsNullOrEmpty(course.Image))
            {
                DeleteOldImage(course.Image);
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // Helper to save image
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/" + uniqueFileName;
        }

        // Helper to delete image
        private void DeleteOldImage(string imagePath)
        {
            if (imagePath.StartsWith("/"))
            {
                imagePath = imagePath.TrimStart('/');
            }
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch (Exception)
                {
                    // Ignore file delete errors
                }
            }
        }
    }

    public class CategoryCreateRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
