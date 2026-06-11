using System.Collections.Generic;

namespace GK_Web.Models
{
    public class CourseListViewModel
    {
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? Credits { get; set; }
        
        // IDs of courses the current logged in student has enrolled in
        public HashSet<int> EnrolledCourseIds { get; set; } = new HashSet<int>();
    }
}
