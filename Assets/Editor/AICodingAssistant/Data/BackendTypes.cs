namespace AICodingAssistant.Data
{
    /// <summary>
    /// Defines the types of powerful, creative backends available.
    /// </summary>
    public enum MainBackendType
    {
        Grok,
        Claude,
        Gemini
    }

    /// <summary>
    /// Defines the types of fast, planning/integration backends available.
    /// </summary>
    public enum PlanningBackendType
    {
        LocalLLM,
        GeminiFlash
    }
}
