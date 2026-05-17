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
        private Label nGame = new Label(), nHib = new Label(), nDef = new Label();
        private Label vH = new Label(), vV = new Label(), vG = new Label(), vM = new Label();
        private Label vGame = new Label(), vHib = new Label(), vDef = new Label();
        private ProgressBar progressBar = new ProgressBar();
        private List<Button> botoesMenu = new List<Button>();
        private static Mutex mutex = null;
        private bool estaProcessando = false;
        private System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();

        [DllImport("user32.dll")] 
        private extern static void ReleaseCapture();

        [DllImport("user32.dll")] 
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const uint SHERB_NOSOUND = 0x00000004;

        protected override CreateParams CreateParams
        {
            get 
            { 
                CreateParams cp = base.CreateParams; 
                cp.ExStyle |= 0x02000000; 
                return cp; 
            }
        }

        public Form1()
        {
            InitializeComponent();
            mutex = new Mutex(true, "C4Otimizador_V3_Final_Stable", out bool createdNew);
            if (!createdNew) { Application.Exit(); return; }

            this.Opacity = 0;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

            ConfigurarInterface();
            AtualizarTodosStatus();
            ValidarBuildSistema();

            refreshTimer.Interval = 3000;
            refreshTimer.Tick += (s, e) => { if (!estaProcessando) AtualizarTodosStatus(); };
            refreshTimer.Start();

            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); } };
            this.Shown += async (s, e) => { await Task.Delay(100); this.Opacity = 1; };
        }

        private void ValidarBuildSistema()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key == null) return;
                    string build = key.GetValue("CurrentBuild")?.ToString() ?? "0";
                    if (build == "19045") { lblStatus.Text = "SISTEMA COMPATÍVEL (WIN 10 22H2)"; lblStatus.ForeColor = Color.Yellow; }
                    else { lblStatus.Text = "SISTEMA INCOMPATÍVEL (BUILD " + build + ")"; lblStatus.ForeColor = Color.Red; }
                }
            }
            catch { lblStatus.Text = "ERRO NA VALIDAÇÃO"; lblStatus.ForeColor = Color.Orange; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(2, 5, 2));
            using (Pen p = new Pen(Color.FromArgb(120, Color.Lime), 2))
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
        }

        private void ConfigurarInterface()
        {
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            this.FormBorderStyle = FormBorderStyle.None; this.Size = new Size(550, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(2, 5, 2);

            btnFechar.Text = "X"; btnFechar.Size = new Size(35, 35); btnFechar.Location = new Point(505, 10);
            btnFechar.FlatStyle = FlatStyle.Flat; btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.ForeColor = Color.White; btnFechar.BackColor = Color.Transparent;
            btnFechar.FlatAppearance.MouseOverBackColor = Color.Red;
            btnFechar.Click += (s, e) => { refreshTimer.Stop(); Application.Exit(); };
            this.Controls.Add(btnFechar);

            lblTitulo.Text = "C4 OTIMIZADOR v3.2"; lblTitulo.ForeColor = Color.White;
            lblTitulo.Font = new Font("Segoe UI", 20F, FontStyle.Bold); lblTitulo.Location = new Point(0, 35);
            lblTitulo.Size = new Size(550, 45); lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblTitulo.BackColor = Color.Transparent; this.Controls.Add(lblTitulo);

            int c1 = 50, c2 = 295, yI = 105, sp = 50;

            CriarBotaoDash("Backup", c1, yI, Color.FromArgb(33, 150, 243), async (s, e) => await WrapperAsync(ExecutarBackup(), "Backup"));
            CriarBotaoDash("Otimizar Mouse", c1, yI + sp, Color.FromArgb(76, 175, 80), async (s, e) => await WrapperAsync(InjetarRegistro(), "Mouse"));
            CriarBotaoDash("Limpeza", c1, yI + (sp * 2), Color.FromArgb(156, 39, 176), async (s, e) => await WrapperAsync(ExecutarLimpeza(), "Limpeza"));
            CriarBotaoDash("Otimizar Rede", c1, yI + (sp * 3), Color.Gold, async (s, e) => await WrapperAsync(OtimizarRede(), "Rede"));
            CriarBotaoDash("Máximo Desempenho", c1, yI + (sp * 4), Color.FromArgb(255, 87, 34), async (s, e) => await WrapperAsync(AtivarTurboMode(), "Energia"));
            CriarBotaoDash("Defender OFF", c1, yI + (sp * 5), Color.FromArgb(183, 28, 28), async (s, e) => await WrapperAsync(DesativarAntivirus(), "Defender"));

            CriarBotaoDash("Desativar Hyper-V", c2, yI, Color.FromArgb(255, 152, 0), async (s, e) => await WrapperAsync(DesativarHyperV(), "Hyper-V"));
            CriarBotaoDash("Desativar VBS", c2, yI + sp, Color.FromArgb(121, 85, 72), async (s, e) => await WrapperAsync(DesativarVBS(), "VBS"));
            CriarBotaoDash("Desativar HAGS", c2, yI + (sp * 2), Color.FromArgb(0, 188, 212), async (s, e) => await WrapperAsync(DesativarHAGS(), "HAGS"));
            CriarBotaoDash("Desativar MPO", c2, yI + (sp * 3), Color.FromArgb(255, 0, 120), async (s, e) => await WrapperAsync(DesligarMPO(), "MPO"));
            CriarBotaoDash("Tema Escuro", c2, yI + (sp * 4), Color.FromArgb(63, 81, 181), async (s, e) => await WrapperAsync(AplicarDarkMode(), "Interface"));
            CriarBotaoDash("Restaurar Padrão", c2, yI + (sp * 5), Color.FromArgb(244, 67, 54), async (s, e) => await WrapperAsync(RestaurarTudo(), "Reset"));

            // BOTÃO PIX
            CriarBotaoDash("Apoiar Projeto (PIX)", 175, 410, Color.DeepPink, (s, e) => MostrarPix());

            // GRADE DE STATUS ORGANIZADA (Cálculo Cirúrgico de Coordenadas)
            int yStatus = 475; // Ponto inicial após o botão PIX
            int linhaH = 22;  // Espaço entre linhas

            CriarLinhaStatus(vM, nM, "DWM MPO:", yStatus);
            CriarLinhaStatus(vG, nG, "GPU HAGS:", yStatus + linhaH);
            CriarLinhaStatus(vV, nV, "VBS KERNEL:", yStatus + (linhaH * 2));
            CriarLinhaStatus(vH, nH, "HYPER-V:", yStatus + (linhaH * 3));
            CriarLinhaStatus(vGame, nGame, "GAME MODE:", yStatus + (linhaH * 4));
            CriarLinhaStatus(vHib, nHib, "HIBERNAÇÃO:", yStatus + (linhaH * 5));
            CriarLinhaStatus(vDef, nDef, "DEFENDER:", yStatus + (linhaH * 6));

            lblStatus.Text = "AGUARDANDO AÇÃO..."; lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(0, 635); lblStatus.Size = new Size(550, 20);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter; lblStatus.BackColor = Color.Transparent;
            this.Controls.Add(lblStatus);

            progressBar.Size = new Size(450, 14); progressBar.Location = new Point(50, 665);
            this.Controls.Add(progressBar);
        }


        private void MostrarPix()
        {
            Form f = new Form { Text = "Apoiar Projeto", Size = new Size(320, 380), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedToolWindow, BackColor = Color.FromArgb(2, 5, 2) };
            PictureBox p = new PictureBox { Image = Properties.Resources.pix, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
            f.Controls.Add(p); f.ShowDialog();
        }

        private void CriarLinhaStatus(Label v, Label n, string t, int y)
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
            int r = 15; b.Region = new Region(new Rectangle(0, 0, b.Width, b.Height));
            void RefreshRegion() { if (b.Width < r || b.Height < r) return; var oldRegion = b.Region; using (GraphicsPath gp = new GraphicsPath()) { gp.AddArc(0, 0, r, r, 180, 90); gp.AddArc(b.Width - r, 0, r, r, 270, 90); gp.AddArc(b.Width - r, b.Height - r, r, r, 0, 90); gp.AddArc(0, b.Height - r, r, r, 90, 90); b.Region = new Region(gp); } oldRegion?.Dispose(); }
            b.Resize += (s, e) => RefreshRegion(); RefreshRegion();
            b.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath gp = new GraphicsPath()) { gp.AddArc(1, 1, r, r, 180, 90); gp.AddArc(b.Width - r - 1, 1, r, r, 270, 90); gp.AddArc(b.Width - r - 1, b.Height - r - 1, r, r, 0, 90); gp.AddArc(1, b.Height - r - 1, r, r, 90, 90); using (Pen pen = new Pen(Color.FromArgb(150, Color.Lime), 1.5f)) e.Graphics.DrawPath(pen, gp); } };
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
            Color corOriginal = lblStatus.ForeColor; string textoOriginal = lblStatus.Text;
            if (this.InvokeRequired) { this.Invoke(new Action(() => { lblStatus.Text = "Processando " + t + "..."; lblStatus.ForeColor = Color.White; })); }
            else { lblStatus.Text = "Processando " + t + "..."; lblStatus.ForeColor = Color.White; }
            lblStatus.Refresh(); progressBar.Value = 100;
            try { await operacao; await Task.Delay(200); AtualizarTodosStatus(); }
            finally { estaProcessando = false; progressBar.Value = 0; lblStatus.Text = textoOriginal; lblStatus.ForeColor = corOriginal; }
        }

        private void AtualizarTodosStatus()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(AtualizarTodosStatus)); return; }
            CheckReg(vH, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
            CheckReg(vV, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
            CheckReg(vG, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
            CheckMPO();
            CheckReg(vGame, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, true);
            CheckReg(vHib, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 1);
            CheckReg(vDef, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1, false, true);
        }

        private void CheckReg(Label v, string p, string k, int onV, bool isHKCU = false, bool isDefender = false)
        {
            try {
                RegistryKey baseKey = isHKCU ? Registry.CurrentUser : Registry.LocalMachine;
                using (var key = baseKey.OpenSubKey(p)) {
                    if (key == null) { v.Text = "OFFLINE"; v.ForeColor = Color.White; return; }
                    int val = Convert.ToInt32(key.GetValue(k) ?? 0); bool isOn = (val == onV);
                    v.Text = isOn ? "ATIVADO" : "DESATIVADO";
                    if (isDefender) { v.Text = isOn ? "DESATIVADO" : "ATIVADO"; v.ForeColor = isOn ? Color.Lime : Color.Red; }
                    else if (k == "AutoGameModeEnabled") { v.ForeColor = isOn ? Color.Lime : Color.Red; }
                    else { v.ForeColor = isOn ? Color.Red : Color.Lime; }
                }
            } catch { v.Text = "OFFLINE"; v.ForeColor = Color.White; }
        }

        private void CheckMPO()
        {
            try { using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm")) { if (key == null) return; int val = Convert.ToInt32(key.GetValue("OverlayTestMode") ?? 0); vM.Text = (val == 5) ? "DESATIVADO" : "ATIVADO"; vM.ForeColor = (val == 5) ? Color.Lime : Color.Red; } }
            catch { vM.Text = "OFFLINE"; vM.ForeColor = Color.White; }
        }

        private async Task<bool> ExecutarVerificadoAsync(string c, string a) { try { using (Process p = new Process()) { p.StartInfo = new ProcessStartInfo { FileName = c, Arguments = a.Trim(), CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }; p.Start(); await Task.Run(() => p.StandardOutput.ReadToEnd()); await Task.Run(() => p.WaitForExit()); return true; } } catch { return false; } }
        private bool Gravar(RegistryKey r, string p, string n, object v, RegistryValueKind t) { try { using (var k = r.CreateSubKey(p)) { k.SetValue(n, v, t); return true; } } catch { return false; } }

        private async Task InjetarRegistro() { await Task.Run(async () => { Gravar(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String); Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters", "MouseDataQueueSize", 25, RegistryValueKind.DWord); await ExecutarVerificadoAsync("powercfg", "/setacvalueindex scheme_current sub_buttons usb-suspend 0"); await ExecutarVerificadoAsync("powercfg", "/setactive scheme_current"); }); FinalizarProcesso("Mouse Otimizado!", "Sucesso", true); }
        private async Task OtimizarRede() { await Task.Run(async () => { Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord); await ExecutarVerificadoAsync("ipconfig", "/flushdns"); Gravar(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord); Gravar(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord); }); FinalizarProcesso("Rede Gamer Otimizada!", "Sucesso", true); }
        private async Task AtivarTurboMode() { await Task.Run(async () => { string turbo = "e9a42b02-d5df-448d-aa00-03f14749eb61"; await ExecutarVerificadoAsync("powercfg", "/delete " + turbo); await ExecutarVerificadoAsync("powercfg", "/restoredefaultschemes"); await ExecutarVerificadoAsync("powercfg", "/duplicatescheme " + turbo); await ExecutarVerificadoAsync("powercfg", "-h off"); await ExecutarVerificadoAsync("powercfg", "/setactive " + turbo); }); FinalizarProcesso("Energia Turbo Ativada!", "Sucesso", false); }
        private async Task DesativarHyperV() { await ExecutarVerificadoAsync("bcdedit", "/set hypervisorlaunchtype off"); await ExecutarVerificadoAsync("DISM", "/Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart"); FinalizarProcesso("Hyper-V Desativado!", "Sucesso", true); }
        private async Task DesativarVBS() { await Task.Run(() => Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord)); FinalizarProcesso("VBS Kernel Desativado!", "Sucesso", true); }
        private async Task DesativarHAGS() { await Task.Run(() => { Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord); }); FinalizarProcesso("GPU HAGS Desativado!", "Sucesso", true); }
        private async Task DesligarMPO() { await Task.Run(() => Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, RegistryValueKind.DWord)); FinalizarProcesso("DWM MPO Desativado!", "Sucesso", true); }
        private async Task ExecutarBackup() { await ExecutarVerificadoAsync("powershell", "Enable-ComputerRestore -Drive 'C:'"); bool ok = await ExecutarVerificadoAsync("powershell", "Checkpoint-Computer -Description 'C4' -RestorePointType MODIFY_SETTINGS"); FinalizarProcesso(ok ? "Backup Criado!" : "Falha no Backup.", "Sistema", false); }
        private async Task AplicarDarkMode() { await Task.Run(() => Gravar(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0, RegistryValueKind.DWord)); FinalizarProcesso("Tema Dark Aplicado!", "Sucesso", false); }
        private async Task ExecutarLimpeza() { await Task.Run(() => { List<string> caminhos = new List<string> { Path.GetTempPath(), Path.Combine(Environment.GetEnvironmentVariable("windir"), "Temp"), Path.Combine(Environment.GetEnvironmentVariable("windir"), "Prefetch"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\DirectX Shader Cache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\GLCache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AMD\DXCache") }; foreach (string path in caminhos) { if (Directory.Exists(path)) { DirectoryInfo di = new DirectoryInfo(path); foreach (FileInfo f in di.GetFiles()) try { f.Delete(); } catch { } foreach (DirectoryInfo d in di.GetDirectories()) try { d.Delete(true); } catch { } } } try { SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOSOUND); } catch { } }); FinalizarProcesso("Limpeza Concluída!", "Sucesso", false); }
        private async Task DesativarAntivirus() { await Task.Run(async () => { string pol = @"SOFTWARE\Policies\Microsoft\Windows Defender"; Gravar(Registry.LocalMachine, pol, "DisableAntiSpyware", 1, RegistryValueKind.DWord); Gravar(Registry.LocalMachine, pol + @"\Real-Time Protection", "DisableRealtimeMonitoring", 1, RegistryValueKind.DWord); using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) { if (k?.GetValue("SecurityHealth") != null) k.DeleteValue("SecurityHealth", false); } await ExecutarVerificadoAsync("powershell", "Set-MpPreference -DisableRealtimeMonitoring $true -DisableBehaviorMonitoring $true"); foreach (var p in Process.GetProcessesByName("SecurityHealthSystray")) try { p.Kill(); } catch { } }); FinalizarProcesso("Defender Desativado!", "Sucesso", true); }
        private async Task RestaurarTudo() { await Task.Run(async () => { await ExecutarVerificadoAsync("powercfg", "-h on"); await ExecutarVerificadoAsync("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm", true)) k?.DeleteValue("OverlayTestMode", false); using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender", true)) { k?.DeleteValue("DisableAntiSpyware", false); try { k?.DeleteSubKeyTree("Real-Time Protection", false); } catch { } } Gravar(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth", @"%windir%\system32\SecurityHealthSystray.exe", RegistryValueKind.ExpandString); Gravar(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 0, RegistryValueKind.DWord); Gravar(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1", RegistryValueKind.String); }); FinalizarProcesso("Sistema Restaurado!", "Sucesso", true); }

        private void FinalizarProcesso(string m, string t, bool r)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => FinalizarProcesso(m, t, r))); return; }
            lblStatus.Text = m; MessageBox.Show(this, m, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (r && MessageBox.Show("Deseja reiniciar agora?", t, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Process.Start("shutdown", "/r /t 0");
            ValidarBuildSistema();
        }
    }
}
