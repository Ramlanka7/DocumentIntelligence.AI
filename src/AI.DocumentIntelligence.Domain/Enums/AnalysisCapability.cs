namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>The kind of AI-driven analysis requested for an <see cref="Entities.AnalysisSession"/>.</summary>
public enum AnalysisCapability
{
    /// <summary>High-level executive summary of the document(s).</summary>
    ExecutiveSummary = 0,

    /// <summary>The most important insights and findings.</summary>
    KeyInsights = 1,

    /// <summary>Actionable items extracted from the document(s).</summary>
    ActionItems = 2,

    /// <summary>Identification and assessment of risks.</summary>
    RiskAssessment = 3,

    /// <summary>Review against compliance and regulatory concerns.</summary>
    ComplianceReview = 4,

    /// <summary>Financial figures, terms, and analysis.</summary>
    FinancialAnalysis = 5,

    /// <summary>Sentiment and tone analysis.</summary>
    SentimentAnalysis = 6,

    /// <summary>A free-form, user-supplied question answered over the document(s).</summary>
    CustomQuestion = 7,
}
