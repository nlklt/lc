using lc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.Helpers
{
    public sealed class CommentItem
    {
        public CommentItem(Comment comment, bool canDelete, bool canBlock)
        {
            Comment = comment ?? throw new ArgumentNullException(nameof(comment));
            CanDelete = canDelete;
            CanBlock = canBlock;
        }

        public Comment Comment { get; }
        public bool CanDelete { get; }
        public bool CanBlock { get; }

        public string UserName => Comment.User?.UserName ?? "Неизвестный";
        public string Text => Comment.Text;
        public DateTime CreatedAt => Comment.CreatedAt;
    }
}
