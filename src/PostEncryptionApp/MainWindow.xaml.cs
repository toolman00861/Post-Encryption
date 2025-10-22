using System;
using System.IO;
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
using Microsoft.Win32;

namespace PostEncryptionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string? selectedFilePath;
    private string? saveFilePath;
    private bool isFileMode = false;
    
    // 文件大小限制：100MB
    private const long MaxFileSize = 100 * 1024 * 1024;
    
    // 支持的文件类型
    private readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp"
    };
    
    private readonly HashSet<string> SupportedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v"
    };

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
        var keyText = KeyTextBox.Text ?? string.Empty;
        
        // 检查密钥是否为占位符文本
        if (keyText == "请输入加密密钥..." || string.IsNullOrWhiteSpace(keyText))
        {
            ShowErrorAnimation(EncryptButton);
            return;
        }

        try
        {
            if (isFileMode)
            {
                // 文件加密模式
                if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    ShowErrorAnimation(EncryptButton);
                    return;
                }

                if (string.IsNullOrEmpty(saveFilePath))
                {
                    ShowErrorAnimation(EncryptButton);
                    return;
                }

                // 读取文件数据
                var fileData = File.ReadAllBytes(selectedFilePath);
                
                // 加密文件数据
                var encryptedData = EncryptData(fileData, keyText);
                
                // 保存加密后的文件
                File.WriteAllBytes(saveFilePath, encryptedData);
                
                MessageTextBox.Text = $"文件已成功加密并保存到：{saveFilePath}";
                MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                // 文本加密模式
                var plaintext = MessageTextBox.Text ?? string.Empty;
                
                if (plaintext == "在此输入要加密或解密的消息内容..." || string.IsNullOrWhiteSpace(plaintext))
                {
                    ShowErrorAnimation(EncryptButton);
                    return;
                }

                var plainBytes = Encoding.UTF8.GetBytes(plaintext);
                var encryptedData = EncryptData(plainBytes, keyText);
                
                MessageTextBox.Text = Convert.ToBase64String(encryptedData);
                MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
            
            ShowSuccessAnimation(EncryptButton);
        }
        catch (Exception)
        {
            ShowErrorAnimation(EncryptButton);
        }
    }

    private void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        var keyText = KeyTextBox.Text ?? string.Empty;
        
        // 检查密钥是否为占位符文本
        if (keyText == "请输入加密密钥..." || string.IsNullOrWhiteSpace(keyText))
        {
            ShowErrorAnimation(DecryptButton);
            return;
        }

        try
        {
            if (isFileMode)
            {
                // 文件解密模式
                if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    ShowErrorAnimation(DecryptButton);
                    return;
                }

                if (string.IsNullOrEmpty(saveFilePath))
                {
                    ShowErrorAnimation(DecryptButton);
                    return;
                }

                // 读取加密文件数据
                var encryptedData = File.ReadAllBytes(selectedFilePath);
                
                // 解密文件数据
                var decryptedData = DecryptData(encryptedData, keyText);
                
                // 保存解密后的文件
                File.WriteAllBytes(saveFilePath, decryptedData);
                
                MessageTextBox.Text = $"文件已成功解密并保存到：{saveFilePath}";
                MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                // 文本解密模式
                var input = MessageTextBox.Text ?? string.Empty;
                
                if (input == "在此输入要加密或解密的消息内容..." || string.IsNullOrWhiteSpace(input))
                {
                    ShowErrorAnimation(DecryptButton);
                    return;
                }

                var encryptedData = Convert.FromBase64String(input);
                var decryptedData = DecryptData(encryptedData, keyText);
                
                MessageTextBox.Text = Encoding.UTF8.GetString(decryptedData);
                MessageTextBox.Foreground = new SolidColorBrush(Colors.White);
            }
            
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

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio)
        {
            isFileMode = radio.Name == "FileModeRadio";
            
            if (TextInputCard != null && FileInputCard != null && FileSaveCard != null)
            {
                TextInputCard.Visibility = isFileMode ? Visibility.Collapsed : Visibility.Visible;
                FileInputCard.Visibility = isFileMode ? Visibility.Visible : Visibility.Collapsed;
                FileSaveCard.Visibility = isFileMode ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择要加密的文件",
            Filter = "所有文件 (*.*)|*.*|图片文件 (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|视频文件 (*.mp4;*.avi;*.mov;*.wmv;*.flv)|*.mp4;*.avi;*.mov;*.wmv;*.flv",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            
            // 检查文件大小
            if (fileInfo.Length > MaxFileSize)
            {
                MessageBox.Show($"文件大小超过限制！最大支持 {FormatFileSize(MaxFileSize)} 的文件。", 
                    "文件过大", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 检查文件类型（可选，这里允许所有文件类型）
            var extension = fileInfo.Extension;
            var isImage = SupportedImageTypes.Contains(extension);
            var isVideo = SupportedVideoTypes.Contains(extension);
            var fileTypeDescription = isImage ? "图片文件" : isVideo ? "视频文件" : "其他文件";
            
            selectedFilePath = openFileDialog.FileName;
            FilePathTextBox.Text = selectedFilePath;
            
            // 显示文件信息
            FileNameText.Text = fileInfo.Name;
            FileSizeText.Text = FormatFileSize(fileInfo.Length);
            FileTypeText.Text = $"{extension.ToUpper()} ({fileTypeDescription})";
            FileInfoPanel.Visibility = Visibility.Visible;
        }
    }

    private void SelectSavePathButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "选择保存位置",
            Filter = "所有文件 (*.*)|*.*",
            DefaultExt = "enc"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            saveFilePath = saveFileDialog.FileName;
            SavePathTextBox.Text = saveFilePath;
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private byte[] EncryptData(byte[] data, string keyText)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyText));
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var cipherBytes = encryptor.TransformFinalBlock(data, 0, data.Length);

        var output = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, output, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, output, aes.IV.Length, cipherBytes.Length);

        return output;
    }

    private byte[] DecryptData(byte[] encryptedData, string keyText)
    {
        if (encryptedData.Length < 16)
        {
            throw new ArgumentException("Invalid encrypted data");
        }

        var iv = new byte[16];
        var cipherBytes = new byte[encryptedData.Length - iv.Length];
        Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, iv.Length, cipherBytes, 0, cipherBytes.Length);

        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyText));
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }
}