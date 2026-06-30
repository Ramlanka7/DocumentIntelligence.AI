namespace AI.DocumentIntelligence.Application.Contracts.AI;

/// <summary>The role a message plays in an AI conversation.</summary>
public enum AiRole
{
    /// <summary>System/developer instruction that steers the model.</summary>
    System = 0,

    /// <summary>Input from the end user.</summary>
    User = 1,

    /// <summary>A response produced by the model.</summary>
    Assistant = 2,
}
