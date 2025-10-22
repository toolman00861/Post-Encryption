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

namespace PostEncryptionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        var plaintext = MessageTextBox.Text ?? string.Empty;
        var keyText = KeyTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(plaintext) || string.IsNullOrWhiteSpace(keyText))
        {
            MessageBox.Show("请填写报文和密钥。");
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
        }
        catch (Exception ex)
        {
            MessageBox.Show("加密失败：" + ex.Message);
        }
    }

    private void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        var input = MessageTextBox.Text ?? string.Empty;
        var keyText = KeyTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keyText))
        {
            MessageBox.Show("请填写报文和密钥。");
            return;
        }

        try
        {
            var allBytes = Convert.FromBase64String(input);
            if (allBytes.Length < 16)
            {
                MessageBox.Show("输入格式错误：密文过短。");
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
        }
        catch (FormatException)
        {
            MessageBox.Show("输入不是有效的Base64文本。");
        }
        catch (CryptographicException)
        {
            MessageBox.Show("解密失败：密钥不正确或数据损坏。");
        }
        catch (Exception ex)
        {
            MessageBox.Show("解密失败：" + ex.Message);
        }
    }
}