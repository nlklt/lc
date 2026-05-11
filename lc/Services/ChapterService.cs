using lc.Data.Repositories.Interfaces;
using lc.Infrastructure.Repositories.Abstractions;
using lc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.Services
{
    internal class ChapterService
    {
        private readonly IChapterRepository _chapterRepository;

        public ChapterService(IChapterRepository chapterRepository)
        {
            _chapterRepository = chapterRepository ?? throw new ArgumentNullException(nameof(chapterRepository));
        }
        public Task<List<Chapter>> GetByBookIdAsync(int bookId)
            => _chapterRepository.GetByBookIdAsync(bookId);
    }
}
