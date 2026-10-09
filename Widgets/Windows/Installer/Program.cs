using System.Drawing;
using System.Windows.Forms;
using Weather.Localization;

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
    private readonly StringLocalizer _strings = StringLocalizer.Current;
    private readonly Button _install = new();
    private readonly Label _status = new();

    public InstallerForm()
    {
        Text = _strings.Get("InstallerWindowTitle");
        ClientSize = new Size(520, 245);
        MinimumSize = Size;
        MaximumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);

        var title = new Label
        {
            Text = _strings.Get("InstallerTitle"),
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Location = new Point(20, 20), Size = new Size(475, 34)
        };
        var explanation = new Label
        {
            Text = _strings.Get("InstallerExplanation"),
            Location = new Point(20, 70), Size = new Size(475, 52)
        };
        var hash = InstallerService.CertificateFingerprint();
        var fingerprint = new TextBox
        {
            Text = _strings.Get("InstallerFingerprint") + Environment.NewLine +
                hash[..32] + " " + hash[32..],
            Location = new Point(20, 129), Size = new Size(475, 43),
            ReadOnly = true, BorderStyle = BorderStyle.None, Multiline = true,
            BackColor = BackColor
        };
        _status.Location = new Point(20, 185);
        _status.Size = new Size(345, 40);
        _install.Text = _strings.Get("InstallerInstall");
        _install.Location = new Point(390, 185);
        _install.Size = new Size(105, 30);
        _install.Click += InstallClicked;
        Controls.AddRange([title, explanation, fingerprint, _status, _install]);
    }

    private async void InstallClicked(object? sender, EventArgs args)
    {
        _install.Enabled = false;
        _status.Text = _strings.Get("InstallerInstalling");
        try
        {
            await Task.Run(InstallerService.InstallAsync);
            _status.Text = _strings.Get("InstallerInstalled");
            MessageBox.Show(this,
                _strings.Get("InstallerSuccess"),
                "AvaWeather", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            _status.Text = _strings.Get("InstallerFailed");
            MessageBox.Show(this, error.Message, "AvaWeather", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _install.Enabled = true; }
    }
}
