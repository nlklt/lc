using System.Windows;
using System.Windows.Controls;

namespace lc.Helpers
{
    public static class PasswordBoxHelper
    {
        // Когда в XAML ставим Attach="True", включается прослушка событий.
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnAttachChanged));
        public static bool GetAttach(DependencyObject obj) => (bool)obj.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject obj, bool value) => obj.SetValue(AttachProperty, value);

        // Свойство для длины пароля (для триггера плейсхолдера в XAML).
        public static readonly DependencyProperty PasswordLengthProperty =
            DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(PasswordBoxHelper),
                new PropertyMetadata(0));
        public static int GetPasswordLength(DependencyObject obj) => (int)obj.GetValue(PasswordLengthProperty);
        public static void SetPasswordLength(DependencyObject obj, int value) => obj.SetValue(PasswordLengthProperty, value);

        // Свойство для хранения самого текста (для отображения пароля при наведении).
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper), 
                new PropertyMetadata(string.Empty));
        public static string GetBoundPassword(DependencyObject obj) => (string)obj.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject obj, string value) => obj.SetValue(BoundPasswordProperty, value);

        // Обработчик изменения Attach. Подписываемся на системное событие PasswordChanged.
        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pb)
            {
                if ((bool)e.NewValue)
                    pb.PasswordChanged += PasswordBox_PasswordChanged;
                else
                    pb.PasswordChanged -= PasswordBox_PasswordChanged;
            }
        }

        // Метод срабатывает каждый раз, когда нажимается клавиша в поле пароля.
        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                // Обновляем длину (для Watermark)
                SetPasswordLength(pb, pb.Password.Length);
                // Обновляем текст (для RevealTextBox в стиле)
                SetBoundPassword(pb, pb.Password);
            }
        }
    }
}