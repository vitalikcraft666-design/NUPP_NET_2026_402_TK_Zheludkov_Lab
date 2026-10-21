namespace GameStudio.Common.Models;

/// <summary>Рівень (грейд) програміста.</summary>
public enum ProgrammerGrade
{
    Junior,
    Middle,
    Senior,
    Lead
}

/// <summary>Напрям роботи художника.</summary>
public enum ArtSpecialization
{
    Concept,
    Modeling3D,
    Animation,
    UserInterface
}

/// <summary>Етап розробки гри.</summary>
public enum ProjectStatus
{
    Concept,
    Prototype,
    Production,
    Testing,
    Released,
    Cancelled
}

/// <summary>Критичність помилки (бага).</summary>
public enum BugSeverity
{
    Low,
    Medium,
    High,
    Critical
}
