using System.Drawing;
using System.Windows.Forms;

namespace AvaWeather.Widget.Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args is ["--trust-cert"])
        {
            try { InstallerService.TrustCertificateElevated(); }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "AvaWeather", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = 1;
            }
            return;
        }
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private readonly bool _russian = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
    private readonly Button _install = new();
    private readonly Label _status = new();

    public InstallerForm()
    {
        Text = "AvaWeather Widget";
        ClientSize = new Size(520, 245);
        MinimumSize = Size;
        MaximumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);

        var title = new Label
        {
            Text = _russian ? "Установка виджета AvaWeather" : "Install AvaWeather widget",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Location = new Point(20, 20), Size = new Size(475, 34)
        };
        var explanation = new Label
        {
            Text = _russian
                ? "Установщик добавит сертификат подписи в доверенные лица Windows и установит виджет MSIX. Для сертификата потребуется разрешение администратора."
                : "The installer will trust the signing certificate on this PC and install the MSIX widget. Administrator approval is required for the certificate.",
            Location = new Point(20, 70), Size = new Size(475, 52)
        };
        var hash = InstallerService.CertificateFingerprint();
        var fingerprint = new TextBox
        {
            Text = (_russian ? "Отпечаток SHA-256:" : "SHA-256 fingerprint:") + Environment.NewLine +
                hash[..32] + " " + hash[32..],
            Location = new Point(20, 129), Size = new Size(475, 43),
            ReadOnly = true, BorderStyle = BorderStyle.None, Multiline = true,
            BackColor = BackColor
        };
        _status.Location = new Point(20, 185);
        _status.Size = new Size(345, 40);
        _install.Text = _russian ? "Установить" : "Install";
        _install.Location = new Point(390, 185);
        _install.Size = new Size(105, 30);
        _install.Click += InstallClicked;
        Controls.AddRange([title, explanation, fingerprint, _status, _install]);
    }

    private async void InstallClicked(object? sender, EventArgs args)
    {
        _install.Enabled = false;
        _status.Text = _russian ? "Проверка и установка…" : "Verifying and installing…";
        try
        {
            await Task.Run(InstallerService.InstallAsync);
            _status.Text = _russian ? "Виджет установлен." : "Widget installed.";
            MessageBox.Show(this,
                _russian ? "Добавьте AvaWeather в панели виджетов Windows." : "Add AvaWeather to the Windows Widgets Board.",
                "AvaWeather", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            _status.Text = _russian ? "Не удалось установить виджет." : "Widget installation failed.";
            MessageBox.Show(this, error.Message, "AvaWeather", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _install.Enabled = true; }
    }
}
