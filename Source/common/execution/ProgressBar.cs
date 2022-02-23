using System;
using System.Text;
using System.Threading;

namespace Common.Execution
{
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private readonly int blockCount;

        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private double barProgress = 0;
        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        private ConsoleColor foreColor;
        private ConsoleColor backColor;

        // delay between screen updates
        public const int TimeDelay = 500;
		
        public ProgressBar()
        {
            barProgress = 0;
            timer = new Timer(TimerHandler);

            // If the console output is redirected to a file, draw nothing.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }

            foreColor = Console.ForegroundColor;
            backColor = Console.BackgroundColor;

            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;

            blockCount = Console.WindowWidth - 4;
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        public void UpdateBar()
        {
            barProgress += 0.01;
            barProgress = Math.Max(0, Math.Min(1, barProgress));
            Interlocked.Exchange(ref currentProgress, barProgress);

            // Reset progress indication
            if (barProgress == 1)
            {
                barProgress = 0;
            }
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed)
                {
                    return;
                }

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);

                //string text = string.Format("[{0}{1}] {2,3}% {3}",
                //    new string('#', progressBlockCount),
                //    new string('-', blockCount - progressBlockCount),
                //    percent,
                //    animation[animationIndex++ % animation.Length]);

                string text = string.Format("[{0}{1}]",
                    new string('#', progressBlockCount),
                    new string('-', blockCount - progressBlockCount));

                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            int cursorTop = Console.CursorTop;
            int cursorLeft = Console.CursorLeft;

            // locate progress bar at top of window
            //Console.SetCursorPosition(1, 0);

            // locate progress bar at bottom of window
            Console.SetCursorPosition(1, Math.Max(Console.WindowHeight - 1, cursorTop));

            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);

            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder(currentText);
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);

            currentText = text;

            // restore cursor position
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            Console.BackgroundColor = backColor;
            Console.ForegroundColor = foreColor;

            UpdateText(new string(' ', Console.WindowWidth - 1));

            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}