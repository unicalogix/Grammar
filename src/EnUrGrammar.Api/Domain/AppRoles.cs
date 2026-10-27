namespace EnUrGrammar.Api.Domain;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly string[] All = [Admin, Teacher, Student];
}
