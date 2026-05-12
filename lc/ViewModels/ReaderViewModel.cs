using lc.Infrastructure;
using lc.Models;
using lc.Services;
using lc.Services.Interfaces;
using lc.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace lc.ViewModels
{
    public class ReaderViewModel : ViewModelBase
    {
        private readonly IChapterService _chapterService;
        private readonly IBookService _bookService;
        private readonly IDialogService _dialogService;

        private Chapter? _selectedChapter;
        private string _chapterText = string.Empty;
        private string _bookTitle = string.Empty;
        private bool _isLoading;

        public ReaderViewModel(int bookId, int? chapterId = null)
        {
            _bookService = ServiceLocator.BookService;
            _dialogService = ServiceLocator.DialogService;

            _ = InitializeAsync(bookId, chapterId);
        }

        public ObservableCollection<Chapter> Chapters { get; } = new();

        public string BookTitle
        {
            get => _bookTitle;
            set => SetProperty(ref _bookTitle, value);
        }

        public string ChapterText
        {
            get => _chapterText;
            set => SetProperty(ref _chapterText, value);
        }

        public Chapter? SelectedChapter
        {
            get => _selectedChapter;
            set
            {
                if (SetProperty(ref _selectedChapter, value))
                {
                    ChapterText = _selectedChapter?.Text ?? string.Empty;
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private async Task InitializeAsync(int bookId, int? chapterId)
        {
            try
            {
                IsLoading = true;

                var book = await _bookService.GetBookByIdAsync(bookId);
                BookTitle = book?.Title ?? "Ридер";

                var chapters = await _chapterService.GetByBookIdAsync(bookId);

                Chapters.Clear();
                foreach (var chapter in chapters)
                    Chapters.Add(chapter);

                if (Chapters.Count > 0)
                {
                    SelectedChapter = chapterId.HasValue
                        ? Chapters.FirstOrDefault(x => x.ChapterId == chapterId.Value) ?? Chapters[0]
                        : Chapters[0];
                }
            }
            catch (System.Exception ex)
            {
                await _dialogService.ShowMessageAsync("Ошибка", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}