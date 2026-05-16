using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace C4_Otimizador
{
    public partial class Form1 : Form
    {
        private Button btnFechar = new Button();
        private Label lblStatus = new Label(), lblTitulo = new Label();
        private Label nH = new Label(), nV = new Label(), nG = new Label(), nM = new Label();
        private Label vH = new Label(), vV = new Label(), vG = new Label(), vM = new Label();
        private ProgressBar progressBar = new ProgressBar();
        private List<Button> botoesMenu = new List<Button>();
        private static Mutex mutex = null;
        private System.Windows.Forms.Timer matrixTimer = new System.Windows.Forms.Timer();
        private Random rnd = new Random();
        private float[] yPositions, ySpeeds;
        private const int fontSize = 12;
        private GraphicsPath c4Path = new GraphicsPath();
        private bool estaProcessando = false;

        [DllImport("user32.dll")] private extern static void ReleaseCapture();
        [DllImport("user32.dll")] private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);
        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const uint SHERB_NOSOUND = 0x00000004;

        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
        }

        public Form1()
        {
            mutex = new Mutex(true, "C4Otimizador_V2_Gold_Stable", out bool createdNew);
            if (!createdNew) { Application.Exit(); return; }
            this.Opacity = 0;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            ConfigurarInterface();
            CriarMascaraC4();
            ConfigurarMatrix();
            AtualizarTodosStatus();
            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); } };
            this.Shown += async (s, e) => { await Task.Delay(100); this.Opacity = 1; matrixTimer.Start(); };
        }

        private void CriarMascaraC4()
        {
            if (c4Path != null) c4Path.Dispose();
            c4Path = new GraphicsPath();
            c4Path.AddString("C4", new FontFamily("Arial Black"), (int)FontStyle.Bold, 260, new Point(75, 200), StringFormat.GenericDefault);
        }

        private void ConfigurarMatrix()
        {
            int columns = 137; // 550 / 4
            yPositions = new float[columns];
            ySpeeds = new float[columns];
            for (int x = 0; x < columns; x++) { yPositions[x] = rnd.Next(-800, 0); ySpeeds[x] = (float)(rnd.NextDouble() * 6 + 7); }
            matrixTimer.Interval = 15;
            matrixTimer.Tick += (s, e) => {
                for (int i = 0; i < yPositions.Length; i++)
                {
                    yPositions[i] += ySpeeds[i];
                    if (yPositions[i] > 720) { yPositions[i] = -rnd.Next(30, 400); ySpeeds[i] = (float)(rnd.NextDouble() * 6 + 7); }
                }
                this.Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(2, 5, 2));
            using (Font f = new Font("Consolas", fontSize, FontStyle.Bold))
            {
                for (int i = 0; i < yPositions.Length; i++)
                {
                    int x = i * 5; float y = yPositions[i];
                    bool inside = c4Path.IsVisible(x, y);
                    using (SolidBrush hB = new SolidBrush(inside ? Color.White : Color.FromArgb(100, 160, 255, 160)))
                        e.Graphics.DrawString(rnd.Next(0, 2).ToString(), f, hB, x, y);
                    for (int j = 1; j < 14; j++)
                    {
                        int alpha = 45 - (j * 3); if (alpha < 5) break;
                        Color tC = inside ? Color.FromArgb(alpha + 60, Color.Lime) : Color.FromArgb(alpha, 0, 180, 0);
                        using (SolidBrush tB = new SolidBrush(tC)) e.Graphics.DrawString(rnd.Next(0, 2).ToString(), f, tB, x, y - (j * 13));
                    }
                }
            }
            using (Pen p = new Pen(Color.FromArgb(120, Color.Lime), 2)) e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
        }

        private void ConfigurarInterface()
        {
            this.FormBorderStyle = FormBorderStyle.None; this.Size = new Size(550, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            btnFechar.Text = "X"; btnFechar.Size = new Size(35, 35); btnFechar.Location = new Point(505, 10);
            btnFechar.FlatStyle = FlatStyle.Flat; btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.ForeColor = Color.White; btnFechar.BackColor = Color.Transparent;
            btnFechar.FlatAppearance.MouseOverBackColor = Color.Red;
            btnFechar.Click += (s, e) => { matrixTimer.Stop(); Application.Exit(); };
            this.Controls.Add(btnFechar);

            lblTitulo.Text = "C4 OTIMIZADOR v2.0"; lblTitulo.ForeColor = Color.White;
            lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold); lblTitulo.Location = new Point(0, 25);
            lblTitulo.Size = new Size(550, 40); lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblTitulo.BackColor = Color.Transparent;
            this.Controls.Add(lblTitulo);

            CriarLinhaStatus(nM, vM, "DWM MPO:", 510); CriarLinhaStatus(nG, vG, "GPU HAGS:", 532);
            CriarLinhaStatus(nV, vV, "VBS KERNEL:", 554); CriarLinhaStatus(nH, vH, "HYPER-V:", 576);

            int c1 = 50, c2 = 295;
            CriarBotaoDash("Backup", c1, 90, Color.FromArgb(33, 150, 243), async (s, e) => await WrapperAsync(ExecutarBackup(), "Backup"));
            CriarBotaoDash("Mouse Otimizado", c1, 145, Color.FromArgb(76, 175, 80), async (s, e) => await WrapperAsync(InjetarRegistro(), "Mouse"));
            CriarBotaoDash("Limpeza Total", c1, 200, Color.FromArgb(156, 39, 176), async (s, e) => await WrapperAsync(ExecutarLimpeza(), "Limpeza"));
            CriarBotaoDash("Rede Gamer", c1, 255, Color.Gold, async (s, e) => await WrapperAsync(OtimizarRede(), "Rede"));
            CriarBotaoDash("Máximo Desempenho", c1, 310, Color.FromArgb(255, 87, 34), async (s, e) => await WrapperAsync(AtivarTurboMode(), "Energia"));
            CriarBotaoDash("Defender OFF", c1, 365, Color.FromArgb(183, 28, 28), async (s, e) => await WrapperAsync(DesativarAntivirus(), "Defender"));

            CriarBotaoDash("Desativar Hyper-V", c2, 90, Color.FromArgb(255, 152, 0), async (s, e) => await WrapperAsync(DesativarHyperV(), "Hyper-V"));
            CriarBotaoDash("Desativar VBS", c2, 145, Color.FromArgb(121, 85, 72), async (s, e) => await WrapperAsync(DesativarVBS(), "VBS"));
            CriarBotaoDash("Desativar HAGS", c2, 200, Color.FromArgb(0, 188, 212), async (s, e) => await WrapperAsync(DesativarHAGS(), "HAGS"));
            CriarBotaoDash("Desativar MPO", c2, 255, Color.FromArgb(255, 0, 120), async (s, e) => await WrapperAsync(DesligarMPO(), "MPO"));
            CriarBotaoDash("Tema Escuro", c2, 310, Color.FromArgb(63, 81, 181), async (s, e) => await WrapperAsync(AplicarDarkMode(), "Visual"));
            CriarBotaoDash("Restaurar Padrão", c2, 365, Color.FromArgb(244, 67, 54), async (s, e) => await WrapperAsync(RestaurarTudo(), "Reset"));

            lblStatus.Text = "BUILD OTIMIZADA PARA WINDOWS 10 PRO 22H2."; lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(0, 615); lblStatus.Size = new Size(550, 20);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter; lblStatus.BackColor = Color.Transparent;
            this.Controls.Add(lblStatus);

            progressBar.Size = new Size(450, 14); progressBar.Location = new Point(50, 650);
            progressBar.Style = ProgressBarStyle.Blocks; this.Controls.Add(progressBar);
        }

        private void CriarLinhaStatus(Label n, Label v, string t, int y)
        {
            n.Text = t; n.ForeColor = Color.White; n.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            n.Location = new Point(140, y); n.Size = new Size(130, 20); n.TextAlign = ContentAlignment.MiddleRight;
            n.BackColor = Color.Transparent; this.Controls.Add(n);
            v.Text = "..."; v.ForeColor = Color.Lime; v.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            v.Location = new Point(275, y); v.Size = new Size(150, 20); v.TextAlign = ContentAlignment.MiddleLeft;
            v.BackColor = Color.Transparent; this.Controls.Add(v);
        }

        private void ArredondarBotao(Button b)
        {
            int r = 15; GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, r, r, 180, 90); gp.AddArc(b.Width - r, 0, r, r, 270, 90);
            gp.AddArc(b.Width - r, b.Height - r, r, r, 0, 90); gp.AddArc(0, b.Height - r, r, r, 90, 90);
            b.Region = new Region(gp);
            b.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (Pen p = new Pen(Color.FromArgb(150, Color.Lime), 2)) e.Graphics.DrawPath(p, gp); };
        }

        private void CriarBotaoDash(string t, int x, int y, Color c, EventHandler ev)
        {
            Button b = new Button { Text = t, Size = new Size(200, 42), Location = new Point(x, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(180, c), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, c); b.Click += ev;
            ArredondarBotao(b); botoesMenu.Add(b); this.Controls.Add(b);
        }

        private async Task WrapperAsync(Task operacao, string t)
        {
            if (estaProcessando) return; estaProcessando = true;
            lblStatus.Text = "Processando " + t + "..."; lblStatus.Refresh();
            progressBar.Value = 100; progressBar.Refresh();
            try { await operacao; await Task.Delay(200); AtualizarTodosStatus(); }
            finally { estaProcessando = false; progressBar.Value = 0; }
        }

        private void AtualizarTodosStatus()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(AtualizarTodosStatus)); return; }
            CheckReg(vH, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
            CheckReg(vV, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
            CheckReg(vG, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
            CheckMPO();
        }

        private void CheckReg(Label v, string p, string k, int onV)
        {
            try { using (var key = Registry.LocalMachine.OpenSubKey(p)) { int val = Convert.ToInt32(key?.GetValue(k) ?? 0); bool isOn = (val == onV); v.Text = isOn ? "ATIVADO" : "DESATIVADO"; v.ForeColor = isOn ? Color.Red : Color.Lime; } }
            catch { v.Text = "OFFLINE"; v.ForeColor = Color.White; }
        }

        private void CheckMPO()
        {
            try { using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm")) { int val = Convert.ToInt32(key?.GetValue("OverlayTestMode") ?? 0); bool off = (val == 5); vM.Text = off ? "DESATIVADO" : "ATIVADO"; vM.ForeColor = off ? Color.Lime : Color.Red; } }
            catch { vM.Text = "OFFLINE"; vM.ForeColor = Color.White; }
        }

        private async Task<bool> ExecutarVerificadoAsync(string c, string a) { try { using (Process p = new Process()) { p.StartInfo = new ProcessStartInfo { FileName = c, Arguments = a, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }; p.Start(); await Task.Run(() => p.StandardOutput.ReadToEnd()); await Task.Run(() => p.WaitForExit()); return true; } } catch { return false; } }
        private bool Gravar(RegistryKey r, string p, string n, object v, RegistryValueKind t) { try { using (var k = r.CreateSubKey(p)) { k.SetValue(n, v, t); return true; } } catch { return false; } }

        private async Task InjetarRegistro() { await Task.Run(async () => { Gravar(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String); Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters", "MouseDataQueueSize", 25, RegistryValueKind.DWord); await ExecutarVerificadoAsync("powercfg", "/setacvalueindex scheme_current sub_buttons usb-suspend 0"); await ExecutarVerificadoAsync("powercfg", "/setactive scheme_current"); }); FinalizarProcesso("Mouse e USB Otimizados!", "Sucesso", true); }
        private async Task OtimizarRede() { await Task.Run(async () => { Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord); await ExecutarVerificadoAsync("ipconfig", "/flushdns"); }); FinalizarProcesso("Rede Otimizada!", "Sucesso", true); }

        private async Task AtivarTurboMode()
        {
            await Task.Run(async () => {
                string guidTurbo = "e9a42b02-d5df-448d-aa00-03f14749eb61";
                await ExecutarVerificadoAsync("powercfg", "/delete " + guidTurbo);
                await ExecutarVerificadoAsync("powercfg", "/restoredefaultschemes");
                await ExecutarVerificadoAsync("powercfg", "/duplicatescheme " + guidTurbo);
                await ExecutarVerificadoAsync("powercfg", "-h off");
                await ExecutarVerificadoAsync("powercfg", "/setactive " + guidTurbo);
            });

            FinalizarProcesso("Energia Turbo Reconstruída! Verifique agora o Painel de Controle.", "Sucesso", false);
        }


        private async Task DesativarHyperV() { await ExecutarVerificadoAsync("bcdedit", "/set hypervisorlaunchtype off"); await ExecutarVerificadoAsync("DISM", "/Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart"); FinalizarProcesso("Hyper-V Desativado!", "Sucesso", true); }
        private async Task DesativarVBS() { await Task.Run(() => Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord)); FinalizarProcesso("VBS Kernel Desativado!", "Sucesso", true); }
        private async Task DesativarHAGS() { await Task.Run(() => Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord)); FinalizarProcesso("GPU HAGS Desativado!", "Sucesso", true); }
        private async Task DesligarMPO() { await Task.Run(() => Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, RegistryValueKind.DWord)); FinalizarProcesso("DWM MPO Desativado!", "Sucesso", true); }
        private async Task ExecutarBackup() { await ExecutarVerificadoAsync("powershell", "Enable-ComputerRestore -Drive 'C:'"); bool ok = await ExecutarVerificadoAsync("powershell", "Checkpoint-Computer -Description 'C4' -RestorePointType MODIFY_SETTINGS"); FinalizarProcesso(ok ? "Backup Criado com Sucesso!" : "Falha no Backup.", "Sistema", false); }
        private async Task AplicarDarkMode() { await Task.Run(() => Gravar(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0, RegistryValueKind.DWord)); FinalizarProcesso("Tema Escuro Aplicado!", "Sucesso", false); }

        private async Task ExecutarLimpeza()
        {
            await Task.Run(() => {
                List<string> caminhos = new List<string> {
                    Path.GetTempPath(), Path.Combine(Environment.GetEnvironmentVariable("windir"), "Temp"), Path.Combine(Environment.GetEnvironmentVariable("windir"), "Prefetch"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\DirectX Shader Cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\GLCache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AMD\DXCache")
                };
                foreach (string path in caminhos)
                {
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (FileInfo f in di.GetFiles()) try { f.Delete(); } catch { }
                        foreach (DirectoryInfo d in di.GetDirectories()) try { d.Delete(true); } catch { }
                    }
                }
                try { SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOSOUND); } catch { }
            });
            FinalizarProcesso("Limpeza Concluída!", "Sucesso", false);
        }

        private async Task DesativarAntivirus()
        {
            await Task.Run(async () => {
                string pol = @"SOFTWARE\Policies\Microsoft\Windows Defender";
                Gravar(Registry.LocalMachine, pol, "DisableAntiSpyware", 1, RegistryValueKind.DWord);
                Gravar(Registry.LocalMachine, pol + @"\Real-Time Protection", "DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) { if (k?.GetValue("SecurityHealth") != null) k.DeleteValue("SecurityHealth", false); }
                await ExecutarVerificadoAsync("powershell", "Set-MpPreference -DisableRealtimeMonitoring $true -DisableBehaviorMonitoring $true");
                foreach (var p in Process.GetProcessesByName("SecurityHealthSystray")) try { p.Kill(); } catch { }
            });
            FinalizarProcesso("Defender Desativado!", "Sucesso", true);
        }

        private async Task RestaurarTudo()
        {
            await Task.Run(async () => {
                await ExecutarVerificadoAsync("powercfg", "-h on");
                await ExecutarVerificadoAsync("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm", true)) k?.DeleteValue("OverlayTestMode", false);
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender", true)) { k?.DeleteValue("DisableAntiSpyware", false); try { k?.DeleteSubKeyTree("Real-Time Protection", false); } catch { } }
                Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth", @"%windir%\system32\SecurityHealthSystray.exe", RegistryValueKind.ExpandString);
                Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
                Gravar(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1", RegistryValueKind.String);
            });
            FinalizarProcesso("Sistema Restaurado!", "Sucesso", true);
        }

        private void FinalizarProcesso(string m, string t, bool r)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => FinalizarProcesso(m, t, r))); return; }
            lblStatus.Text = m; MessageBox.Show(this, m, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (r && MessageBox.Show("Deseja reiniciar agora?", t, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Process.Start("shutdown", "/r /t 0");
        }
    }
}
