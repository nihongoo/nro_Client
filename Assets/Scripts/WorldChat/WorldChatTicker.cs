using System;
using System.Collections.Generic;

namespace WorldChat
{
    // The stock ticker moved left by two logical pixels each update, once per message.
    public sealed class WorldChatTicker
    {
        public const int WaitingLimit = 3;
        private readonly Queue<string> waiting = new Queue<string>();
        private int textWidth;
        public string Current { get; private set; }
        public int X { get; private set; }
        public int WaitingCount => waiting.Count;
        public void Reset() { waiting.Clear(); Current = null; X = 0; }
        public void Enqueue(string text)
        {
            if (waiting.Count == WaitingLimit) waiting.Dequeue();
            waiting.Enqueue(text);
        }
        public void Update(int screenWidth, Func<string, int> measure)
        {
            if (Current == null)
            {
                if (waiting.Count == 0) return;
                Current = waiting.Dequeue(); textWidth = measure(Current); X = screenWidth;
                return;
            }
            X -= 2;
            if (X < -textWidth) Current = null;
        }
    }
}
