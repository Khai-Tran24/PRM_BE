namespace BE_SaleHunter.Core.Configuration
{
    /// <summary>
    /// Configuration settings for Ollama AI service integration
    /// </summary>
    public class OllamaConfiguration
    {
        public const string SectionName = "Ollama";

        /// <summary>
        /// The base URL for the Ollama API endpoint
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:11434";

        /// <summary>
        /// The default model to use for chat interactions
        /// </summary>
        public string DefaultModel { get; set; } = "llama3.2:1b";

        /// <summary>
        /// Whether to enable streaming responses from Ollama
        /// </summary>
        public bool EnableStreaming { get; set; } = false;

        /// <summary>
        /// Timeout in seconds for Ollama API requests
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Maximum number of conversation history messages to include in context
        /// </summary>
        public int MaxContextMessages { get; set; } = 10;

        /// <summary>
        /// Whether to enable chain-of-thought thinking extraction
        /// </summary>
        public bool EnableThinkingExtraction { get; set; } = true;

        /// <summary>
        /// Template for system prompt that includes SaleHunter context
        /// </summary>
        public string SystemPromptTemplate { get; set; } = @"You are an AI assistant for SaleHunter, a price comparison mobile application.
SaleHunter helps users find the best prices for products across different stores.
The user's name is {userName}.
{storeInfo}

You should help users with:
- Finding products and comparing prices
- Information about stores and their locations
- General questions about using the SaleHunter app
- Product recommendations and shopping advice

When answering, think through your response step by step by wrapping your reasoning in <think></think> tags.
Only provide the final answer outside of the thinking tags.";

        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new InvalidOperationException("Ollama Endpoint cannot be null or empty");

            if (string.IsNullOrWhiteSpace(DefaultModel))
                throw new InvalidOperationException("Ollama DefaultModel cannot be null or empty");

            if (TimeoutSeconds <= 0)
                throw new InvalidOperationException("Ollama TimeoutSeconds must be greater than 0");

            if (MaxContextMessages < 0)
                throw new InvalidOperationException("Ollama MaxContextMessages cannot be negative");

            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
                throw new InvalidOperationException("Ollama Endpoint must be a valid URL");
        }
    }
}
