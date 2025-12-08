namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// POCO representing a single step in the marking rubric.
    /// This is serialized into SubjectiveRubric.StepsJson.
    /// </summary>
    public class StepRubricItem
    {
        /// <summary>
        /// Step number (1-based)
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Description of what the student should demonstrate in this step
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Maximum marks for this step
        /// </summary>
        public int Marks { get; set; }
    }
}
