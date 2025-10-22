using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace PostEncryptionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 清空占位符文本当获得焦点时
        MessageTextBox.GotFocus += (s, e) =>
        {
            if (MessageTextBox.Text == "在此输入要加密或解密的消息内容...")
            {
                MessageTextBox.Text = "";
                MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
        };

        MessageTextBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageTextBox.Text = "在此输入要加密或解密的消息内容...";
                MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            }
        };

        KeyTextBox.GotFocus += (s, e) =>
        {
            if (KeyTextBox.Text == "请输入加密密钥...")
            {
                KeyTextBox.Text = "";
                KeyTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
        };

        KeyTextBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(KeyTextBox.Text))
            {
                KeyTextBox.Text = "请输入加密密钥...";
                KeyTextBox.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            }
        };

        // 设置初始占位符颜色
        MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
        KeyTextBox.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
    }

    private void ShowSuccessAnimation(Button button)
    {
        var originalBackground = button.Background;
        var successBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69));
        
        var colorAnimation = new ColorAnimation
        {
            To = Color.FromRgb(40, 167, 69),
            Duration = TimeSpan.FromMilliseconds(200),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(colorAnimation);
        Storyboard.SetTarget(colorAnimation, button);
        Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("Background.Color"));
        
        storyboard.Completed += (s, e) => button.Background = originalBackground;
        storyboard.Begin();
    }

    private void ShowErrorAnimation(Button button)
    {
        var originalBackground = button.Background;
        
        var colorAnimation = new ColorAnimation
        {
            To = Color.FromRgb(220, 53, 69),
            Duration = TimeSpan.FromMilliseconds(200),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(colorAnimation);
        Storyboard.SetTarget(colorAnimation, button);
        Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("Background.Color"));
        
        storyboard.Completed += (s, e) => button.Background = originalBackground;
        storyboard.Begin();
    }

    private void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        var plaintext = MessageTextBox.Text ?? string.Empty;
        var keyText = KeyTextBox.Text ?? string.Empty;
        
        // 检查是否为占位符文本
        if (plaintext == "在此输入要加密或解密的消息内容..." || string.IsNullOrWhiteSpace(plaintext) ||
            keyText == "请输入加密密钥..." || string.IsNullOrWhiteSpace(keyText))
        {
            ShowErrorAnimation(EncryptButton);
            return;
        }

        try
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyText));
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var output = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, output, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, output, aes.IV.Length, cipherBytes.Length);

            MessageTextBox.Text = Convert.ToBase64String(output);
            MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            
            ShowSuccessAnimation(EncryptButton);
        }
        catch (Exception)
        {
            ShowErrorAnimation(EncryptButton);
        }
    }

    private void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        var input = MessageTextBox.Text ?? string.Empty;
        var keyText = KeyTextBox.Text ?? string.Empty;
        
        // 检查是否为占位符文本
        if (input == "在此输入要加密或解密的消息内容..." || string.IsNullOrWhiteSpace(input) ||
            keyText == "请输入加密密钥..." || string.IsNullOrWhiteSpace(keyText))
        {
            ShowErrorAnimation(DecryptButton);
            return;
        }

        try
        {
            var allBytes = Convert.FromBase64String(input);
            if (allBytes.Length < 16)
            {
                ShowErrorAnimation(DecryptButton);
                return;
            }

            var iv = new byte[16];
            var cipherBytes = new byte[allBytes.Length - iv.Length];
            Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(allBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);

            var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyText));
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            MessageTextBox.Text = Encoding.UTF8.GetString(plainBytes);
            MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            
            ShowSuccessAnimation(DecryptButton);
        }
        catch (FormatException)
        {
            ShowErrorAnimation(DecryptButton);
        }
        catch (CryptographicException)
        {
            ShowErrorAnimation(DecryptButton);
        }
        catch (Exception)
        {
            ShowErrorAnimation(DecryptButton);
        }
    }
}