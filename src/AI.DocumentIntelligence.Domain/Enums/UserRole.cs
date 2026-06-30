namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Role governing what a <see cref="Entities.User"/> may do (role-based authorization).</summary>
public enum UserRole
{
    /// <summary>Full administrative access, including the admin dashboard and user management.</summary>
    Admin = 0,

    /// <summary>Can upload documents and run analysis, comparison, and chat.</summary>
    Analyst = 1,

    /// <summary>Read-only access to existing results.</summary>
    Viewer = 2,
}
