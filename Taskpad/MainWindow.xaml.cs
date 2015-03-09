using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace TaskApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool _isLoading;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading)
                return;
            _isLoading = true;

            RichTextBox t = sender as RichTextBox;
            if (t == null)
                return;

            BlockCollection bc = t.Document.Blocks;
            foreach (Block b in bc)
            {
                if (b is Paragraph)
                {
                    var para = b as Paragraph;
                    string text = new TextRange(para.ContentStart, para.ContentEnd).Text;

                    bool isProject = text.EndsWith(":");
                    bool isTask = text.StartsWith("-") || text.StartsWith("*");
                    bool isNote = !isProject && !isTask;
                    bool isDone = text.Contains("@done");

                    para.Foreground = isNote ? Brushes.DimGray : Brushes.Black;
                    para.FontSize = isProject ? 16 : 12;
                    para.TextDecorations = isDone ? TextDecorations.Strikethrough : null;
                }
            }
            _isLoading = false;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox s = (TextBox)sender;
            string searchTerm = s.Text;
            RichTextBox r = ContentBox;
            DoSearch(r, searchTerm, false);

        }

        private static bool DoSearch(RichTextBox richTextBox, string searchText, bool searchNext)
        {
            TextRange searchRange;

            // Get the range to search
            if (searchNext)
                searchRange = new TextRange(
                    richTextBox.Selection.Start.GetPositionAtOffset(1),
                    richTextBox.Document.ContentEnd);
            else
                searchRange = new TextRange(
                    richTextBox.Document.ContentStart,
                    richTextBox.Document.ContentEnd);

            // Do the search
            TextRange foundRange = FindTextInRange(searchRange, searchText);
            if (foundRange == null)
                return false;

            // Select the found range
            richTextBox.Selection.Select(foundRange.Start, foundRange.End);
            return true;
        }

        private static TextRange FindTextInRange(TextRange searchRange, string searchText)
        {
            // Search the text with IndexOf
            int offset = searchRange.Text.IndexOf(searchText);
            if (offset < 0)
                return null;  // Not found

            // Try to select the text as a contiguous range
            for (TextPointer start = searchRange.Start.GetPositionAtOffset(offset); start != null && start != searchRange.End; start = start.GetPositionAtOffset(1))
            {
                TextRange result = new TextRange(start, start.GetPositionAtOffset(searchText.Length));
                if (result.Text == searchText)
                    return result;
            }
            return null;
        }

        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) 
                return;

            TextBox s = (TextBox)sender;
            string searchTerm = s.Text;
            RichTextBox r = ContentBox;
            DoSearch(r, searchTerm, true);
            e.Handled = true;
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, new TextRange(ContentBox.Document.ContentStart, ContentBox.Document.ContentEnd).Text);
            }
        }

        private void ButtonOpen_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() != true) 
                return;

            _isLoading = true;
            ContentBox.Document.Blocks.Clear();
            string[] s = File.ReadAllLines(openFileDialog.FileName);

            string allLines = s.Aggregate("", (current, line) => current + (line + "\n"));

            _isLoading = false;
            ContentBox.AppendText(allLines);
        }

        private void ButtonNew_OnClick(object sender, RoutedEventArgs e)
        {
            ContentBox.Document.Blocks.Clear();
        }
    }
}
