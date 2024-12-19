﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using MPF.Frontend;

namespace MPF.UI.UserControls
{
    public partial class LogOutput : UserControl
    {
        /// <summary>
        /// Document representing the text
        /// </summary>
        private readonly  FlowDocument _document;

        /// <summary>
        /// Queue of items that need to be logged
        /// </summary>
        private readonly ProcessingQueue<LogLine> _logQueue;

        /// <summary>
        /// Paragraph backing the log
        /// </summary>
        private readonly Paragraph _paragraph;

#if NET35

        private Button? ClearButton => ItemHelper.FindChild<Button>(this, "ClearButton");
        private RichTextBox? Output => ItemHelper.FindChild<RichTextBox>(this, "Output");
        private ScrollViewer? OutputViewer => ItemHelper.FindChild<ScrollViewer>(this, "OutputViewer");
        private Button? SaveButton => ItemHelper.FindChild<Button>(this, "SaveButton");

#endif

        public LogOutput()
        {
#if NET40_OR_GREATER || NETCOREAPP
            InitializeComponent();
#endif

            // Update the internal state
            _document = new FlowDocument()
            {
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20))
            };
            _paragraph = new Paragraph();
            _document.Blocks.Add(_paragraph);

            // Setup the processing queue
            _logQueue = new ProcessingQueue<LogLine>(ProcessLogLine);

            // Add handlers
            OutputViewer!.SizeChanged += OutputViewerSizeChanged;
            Output!.TextChanged += OnTextChanged;
            ClearButton!.Click += OnClearButton;
            SaveButton!.Click += OnSaveButton;

            // Update the internal state
            Output.Document = _document;
        }

        #region Logging

        /// <summary>
        /// Enqueue text to the log with formatting
        /// </summary>
        /// <param name="logLevel">LogLevel for the log</param>
        /// <param name="text">Text to write to the log</param>
        public void EnqueueLog(LogLevel logLevel, string text)
        {
            // Null text gets ignored
            if (text == null)
                return;

            // Enqueue the text
            _logQueue.Enqueue(new LogLine(text, logLevel));
        }

        /// <summary>
        /// Log line wrapper
        /// </summary>
        internal readonly struct LogLine
        {
            public readonly string Text;
            public readonly LogLevel LogLevel;

            public LogLine(string text, LogLevel logLevel)
            {
                Text = text;
                LogLevel = logLevel;
            }

            /// <summary>
            /// Get the foreground Brush for the current LogLevel
            /// </summary>
            /// <returns>Brush representing the color</returns>
            public Brush GetForegroundColor()
            {
                return LogLevel switch
                {
                    LogLevel.SECRET => Brushes.Blue,
                    LogLevel.ERROR => Brushes.Red,
                    LogLevel.VERBOSE => Brushes.Yellow,
                    _ => Brushes.White,
                };
            }

            /// <summary>
            /// Generate a Run object from the current LogLine
            /// </summary>
            /// <returns>Run object based on internal values</returns>
            public Run GenerateRun()
            {
                return new Run { Text = Text, Foreground = GetForegroundColor() };
            }
        }

        /// <summary>
        /// Process the log lines in the queue
        /// </summary>
        /// <param name="logLine">LogLine item to process</param>
        internal void ProcessLogLine(LogLine logLine)
        {
            // Null text gets ignored
            string nextText = logLine.Text;
            if (nextText == null)
                return;

            Dispatcher.Invoke(() =>
            {
                var run = logLine.GenerateRun();
                _paragraph.Inlines.Add(run);
            });
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Clear all inlines of the paragraph
        /// </summary>
        private void ClearInlines() => _paragraph.Inlines.Clear();

        /// <summary>
        /// Save all inlines to console.log
        /// </summary>
        private void SaveInlines()
        {
            using var sw = new StreamWriter(File.OpenWrite("console.log"));
            foreach (var inline in _paragraph.Inlines)
            {
                if (inline is Run run)
                    sw.Write(run.Text);
            }
        }

        /// <summary>
        /// Scroll the current view to the bottom
        /// </summary>
        public void ScrollToBottom() => OutputViewer!.ScrollToBottom();

        #endregion

        #region EventHandlers

        private void OnClearButton(object sender, EventArgs e)
            => ClearInlines();

        private void OnSaveButton(object sender, EventArgs e)
            => SaveInlines();

        private void OnTextChanged(object sender, TextChangedEventArgs e)
            => ScrollToBottom();

        private void OutputViewerSizeChanged(object sender, SizeChangedEventArgs e)
            => ScrollToBottom();

        #endregion
    }
}
