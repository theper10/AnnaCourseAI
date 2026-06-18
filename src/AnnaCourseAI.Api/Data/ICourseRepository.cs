using AnnaCourseAI.Api.Models;

namespace AnnaCourseAI.Api.Data;

public interface ICourseRepository
{
    IReadOnlyList<Course> GetCourses();

    Course? GetCourse(string courseId);

    IReadOnlyList<CourseMaterial> GetMaterials(string courseId);

    CourseMaterial AddMaterial(string courseId, AddMaterialRequest request);
}
