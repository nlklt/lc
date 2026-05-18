using lc.Models.Enums;

namespace lc.Helpers;

public sealed class AdminUserRowDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool BlockedComments { get; set; }
    public int PublishedBooksCount { get; set; }
    public int CommentsCount { get; set; }

    public bool CanToggleCommentBlock { get; set; }
    public bool CanDelete { get; set; }

    public string RoleText => Role switch
    {
        UserRole.Admin => "Админ",
        UserRole.Writer => "Автор",
        UserRole.Reader => "Читатель",
        _ => Role.ToString()
    };

    public string BlockedCommentsText => BlockedComments ? "Да" : "Нет";
    public string DeleteText => CanDelete ? "Удалить" : "Нельзя удалить";
    public string CommentBlockText => BlockedComments ? "Разрешить" : "Запретить";
}