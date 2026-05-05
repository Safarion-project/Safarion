using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage; // NEW: Added to access Preferences

namespace Safarion.Services
{
    public static class GeminiService
    {
        // ⚠️ PASTE YOUR KEY HERE
        private const string ApiKey = "KEY";
        private const string EndpointUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

        // Removed the 'const' so we can dynamically add the language to it!
        private static string BaseSystemInstruction =
            "for educational purposes only. markup to text. " +
            "You are Safarion, an intelligent AI travel companion for solo travelers. " +
            "Your goals are: " +
            "1. Safety First: Always prioritize user safety. If a user says 'SOS' or 'Help', give immediate emergency advice. " +
            "2. Local Expert: Recommend authentic local food, cultural etiquette, and hidden gems. " +
            "3. Concise: Keep answers short (under 100 words) and easy to read on a phone. " +
            "5. If asked about festivals, mention popular ones in the region." +
            "4. Tone: Friendly, calm, and protective.";

        public static async Task<string> GetResponse(string userMessage)
        {
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    return "⚠️ No Internet Connection.";

                using var client = new HttpClient();

                // --- NEW: PHASE 2 MULTILINGUAL BRAIN ---
                // 1. Grab the user's language from the phone's memory
                string userLanguage = Preferences.Default.Get("AppLanguage", "English");

                // Inject the strict Token Diet into the AI's brain
                // ---------------------------------------------------------
                string dynamicSystemInstruction = BaseSystemInstruction +
                    $" IMPORTANT: You MUST completely translate your final response into {userLanguage}. " +
                    "If the language is not English, you MUST keep your response EXTREMELY short (under 40 words) to ensure lightning-fast loading times on mobile networks.";
                // ---------------------------------------------------------

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                // Pass the dynamic instruction instead of the static one
                                new { text = $"{dynamicSystemInstruction}\n\nUser: {userMessage}" }
                            }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{EndpointUrl}?key={ApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    return $"AI Error: {response.StatusCode}. Details: {errorDetail}";
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    return candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Thinking...";
                }

                return "I am having trouble thinking right now.";
            }
            catch
            {
                return "I cannot connect to the internet right now. Please check your connection.";
            }
        }
    }
}
