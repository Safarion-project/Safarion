namespace Safarion.Models
{
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }

        // These properties change the look based on who sent the message
        public Color BubbleColor => IsUser ? Color.FromArgb("#00E676") : Color.FromArgb("#E0E0E0"); // Green for you, Grey for AI
        public Color TextColor => IsUser ? Colors.White : Colors.Black;
        public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start; // You on Right, AI on Left
    }
}